﻿using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Selections;
using System.Diagnostics;

namespace Rysy.Tools;
public class PlacementTool : Tool, ISelectionHotkeyTool {
    public ISelectionHandler? CurrentPlacement { get; private set; }
    private object? _currentPlacementSourceMaterial;

    public SelectRectangleGesture RectangleGesture;

    private bool PickNextFrame;

    /// <summary>
    /// Position at which the GetPreviewSprites method will be called, regardless of the RectangleGesture's location. 
    /// </summary>
    private Vector2? AnchorPos;

    /// <summary>
    /// When dragging a placement, specifies which node should be moved.
    /// </summary>
    private int _draggedNodeIndex;

    private bool _shouldDragNodesOfResizableEntity;

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("tools.pick", "mousemiddle", OnMiddleClick);
        this.AddSelectionHotkeys(handler);
    }

    public override string Name => "placement";

    public override string PersistenceGroup => "placement";

    private static List<EditorLayer> _validLayers = new() {
        EditorLayers.Entities,
        EditorLayers.Triggers,
        EditorLayers.FgDecals,
        EditorLayers.BgDecals,
        EditorLayers.Room,
        EditorLayers.Prefabs,
    };
    
    public override List<EditorLayer> ValidLayers => _validLayers;
    
    public override IEnumerable<object>? GetMaterials(EditorLayer layer) {
        return layer.GetMaterials();
    }

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        if (material is Placement pl) {
            if (layer == EditorLayers.Prefabs) {
                return pl.Name;
            }

            var prefix = layer.MaterialLangPrefix;
            var name = prefix is null 
                ? pl.Name 
                : pl.Name.TranslateOrHumanize(Interpolator.Temp($"{prefix}.{pl.SID ?? ""}.placements.name"));

            var associated = pl.GetAssociatedMods();
            if (associated is { Count: > 0 }) {
                return $"{name} [{string.Join(',', associated.Select(ModMeta.ModNameToDisplayName))}]";
            }
            
            if (pl.PlacementHandler.ShowVanillaAsDefiningModInPlacementName())
                return $"{name} [Vanilla]";
            
            return name;
        }

        return material switch {
            string s => s,
            _ => material?.ToString() ?? "null",
        };
    }

    public override string? GetMaterialTooltip(EditorLayer layer, object material) {
        if (material is not Placement pl)
            return null;

        if (pl.Tooltip is { } tooltip)
            return tooltip;
        
        var prefix = layer.MaterialLangPrefix;
        if (prefix is null)
            return null;
        
        return pl.Name.TranslateOrNull($"{prefix}.{pl.SID}.placements.description");
    }

    private static Placement? PlacementFromString(string str, EditorLayer layer) {
        Console.WriteLine($"PlacementFromString: {str} [{layer}]");
        /*
        return layer switch {
            LayerNames.FG_DECALS => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            LayerNames.BG_DECALS => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            LayerNames.PREFABS => PrefabHelper.PlacementFromName(str),
            _ => null,
        };*/
        //TODO:
        return null;
    }

    protected override void OnLayerChanged() {
        base.OnLayerChanged();

        var cache = MaterialPreviewCache;
        MaterialPreviewCache.Clear();
        RysyState.OnEndOfThisFrame += () => {
            foreach (var (k, v) in cache) {
                ImGuiManager.DisposeXnaWidget(k.ToString());
            }
            cache.Clear();
        };
    }

    private void ResetDragState() {
        _draggedNodeIndex = 0;
        _shouldDragNodesOfResizableEntity = false;
    }

    public override void Update(Camera camera, Room? room) {
        // In a room layer, we should always act like no room is selected, as we should use global coords.
        // Otherwise, remaining layers are only usable in a room.
        if (Layer is RoomLayer) {
            room = null;
        } else {
            if (room is null)
                return;
        }
        
        if (PickNextFrame) {
            PickNextFrame = false;
            if (GetPlacementUnderCursor(GetMousePos(camera, room, precise: true), room, EditorLayers.ToolLayerToEnum(Layer)) is { } underCursor) {
                Material = underCursor;
            }
            CurrentPlacement = null;
            _currentPlacementSourceMaterial = null;
        }

        if (CurrentPlacement is not { } placement) {
            ResetDragState();
            CreatePlacementFromMaterial(camera, room);
            return;
        }

        if (Material is Placement place) {
            if (RectangleGesture.Update((p) => GetMousePos(camera, room, position: p.ToVector2())) is { } rect) {
                History.ApplyNewAction(place.PlacementHandler.Place(placement, room!));
                AnchorPos = null;
                ResetDragState();
                if (placement is EntitySelectionHandler entityHandler) {
                    entityHandler.Entity.EntityData.ReplaceNodes(place.Nodes ?? Enumerable.Range(0, entityHandler.Entity.NodeLimits.Start.Value).Select(_ => new Vector2()));
                    entityHandler.Entity.InitializeNodePositions();
                }
            }

            HandleMove(camera, room, placement);
        }
    }

    private void HandleMove(Camera camera, Room? room, ISelectionHandler selection) {
        if (RectangleGesture.Delta is not { } delta)  {
            ResetDragState();
            return;
        }
        
        var offset = delta.Location.ToVector2();
        var resize = delta.Size();
        
        // For placements which start with larger width/height, make sure we only start resizing right/down once the mouse cursor extends past the default size.
        var dragRect = RectangleGesture.CurrentRectangle!.Value;
        if (resize.X > 0 && RectangleGesture.GetTransformedMousePos().X > RectangleGesture.StartPos!.Value.X && selection.Rect.Width > dragRect.Width)
            resize.X = 0;
        if (resize.Y > 0 && RectangleGesture.GetTransformedMousePos().Y > RectangleGesture.StartPos!.Value.Y && selection.Rect.Height > dragRect.Height)
            resize.Y = 0;
        
        if (offset == Vector2.Zero && resize == Point.Zero)
            return;

        var map = room?.Map ?? EditorState.Map!;

        // handle noded entity resizing being different
        // TODO: refactor, maybe into a ICustomMoveHandler
        if (selection is EntitySelectionHandler entityHandler) {
            var e = entityHandler.Entity;

            if ((_shouldDragNodesOfResizableEntity || !(e.ResizableX || e.ResizableY)) && e.Nodes.Count > _draggedNodeIndex) {
                var node = e.Nodes[_draggedNodeIndex];
                new MoveNodeAction(node, e, GetMousePos(camera, room).ToVector2() - node).Apply(map);
                AnchorPos ??= e.Pos;
                return;
            }

            if (offset.X != 0 || offset.Y != 0) {
                selection.MoveBy(offset)?.Apply(map);
            }

            if ((resize.X != 0 || resize.Y != 0) && selection.TryResize(resize) is { } resizeAction) {
                resizeAction.Apply(map);
                e.InitializeNodePositions();
            }

            return;
        }

        if (offset.X != 0 || offset.Y != 0) {
            selection.MoveBy(offset)?.Apply(map);
        }

        if (resize.X != 0 || resize.Y != 0) {
            selection.TryResize(resize)?.Apply(map);
        }
    }

    private void CreatePlacementFromMaterial(Camera camera, Room? currentRoom) {
        if (Material is string strPlacement) {
            Material = PlacementFromString(strPlacement, Layer);
        }

        if (Material is Placement place) {
            // quick actions might not serialize this properly
            if (place.PlacementHandler is null) {
                Material = place.Name;
                return;
            }

            var handler = place.PlacementHandler;
            CurrentPlacement = handler.CreateSelection(place, GetMousePos(camera, currentRoom).ToVector2(), currentRoom!);
            _currentPlacementSourceMaterial = Material;
            if (CurrentPlacement is EntitySelectionHandler entityHandler) {
                entityHandler.Entity.InitializeNodePositions();
            }
        }
    }

    public void OnMiddleClick() {
        PickNextFrame = true;
    }

    internal static Placement? GetPlacementUnderCursor(Point mouse, Room? currentRoom, SelectionLayer layer) {
        var selections = currentRoom?.GetSelectionsInRect(new(mouse.X, mouse.Y, 1, 1), layer);
        if (selections is null || selections.Count == 0)
            return null;

        if (selections[0].Handler.Parent is { } parent && Placement.TryCreateFromObject(parent) is { } placement)
            return placement;

        return null;
    }

    public override void Render(Camera camera, Room? room) {
        if (Layer is RoomLayer && room is {}) {
            return;
        }
        
        var mouse = GetMousePos(camera, room);

        if (Material is Placement placement && CurrentPlacement is { } selection) {
            var pos = AnchorPos ?? (RectangleGesture.CurrentRectangle is { } rect ? rect.Location.ToVector2() : mouse.ToVector2());
            var ctx = SpriteRenderCtx.Default();
            var offset = placement.PlacementHandler.GetPreviewSpritesOffset(selection, pos, room!);
            SpriteBatchState prevState = default;
            if (offset is { }) {
                prevState = GFX.EndBatch()!.Value;
                GFX.BeginBatch(prevState with {
                    TransformMatrix = prevState.TransformMatrix * Matrix.CreateTranslation(offset.Value.X * camera.Scale, offset.Value.Y * camera.Scale, 0f)
                });
            }
            
            foreach (var item in placement.GetPreviewSprites(selection, pos, room!)) {
                if (item is Sprite spr) {
                    spr.RenderWithColor(ctx, spr.Color * 0.4f);
                } else {
                    item.WithMultipliedAlpha(0.4f).Render(ctx);
                }
            }

            if (offset is { }) {
                GFX.EndBatch();
                GFX.BeginBatch(prevState);
            }
        }

        if (!ImGuiManager.WantCaptureMouse && !ImGui.IsAnyItemHovered()) {
            var mousePos = GetMousePos(camera, room);

            var selectionsUnderCursor = room?.GetSelectionsInRect(new Rectangle(mousePos.X, mousePos.Y, 1, 1), EditorLayers.ToolLayerToEnum(Layer));
            
            SelectionTool.HandleHoveredSelections(room, selectionsUnderCursor, selected: null, Input);
        }
    }

    private Point GetMousePos(Camera camera, Room? currentRoom, bool? precise = null, Vector2? position = null) {
        precise ??= Input.Keyboard.Ctrl();

        var pos = position ?? Input.Mouse.Pos.ToVector2();
        pos = currentRoom?.WorldToRoomPos(camera, pos) ?? camera.ScreenToReal(pos);

        if (!precise.Value) {
            pos = pos.Snap(GridSize);
        }

        return pos.ToPoint();
    }

    public override void RenderOverlay() {
        if (Layer is RoomLayer) {
            var camera = EditorState.Camera;
            GFX.EndBatch();
            GFX.BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix));
            Render(camera, null);
        }
    }

    public override void CancelInteraction() {
        base.CancelInteraction();

        if (CurrentPlacement is { } pl && 
            (pl.Layer != Layer.SelectionLayer || _currentPlacementSourceMaterial != Material)) {
            CurrentPlacement = null;
            _currentPlacementSourceMaterial = null;
        }
        PickNextFrame = false;
        AnchorPos = null;
        ResetDragState();
    }

    public override void Init() {
        base.Init();

        PrefabHelper.CurrentPrefabs.OnChanged += ClearMaterialListCache;

        RectangleGesture = new(Input);
    }

    public override void Unload() {
        base.Unload();

        PrefabHelper.CurrentPrefabs.OnChanged -= ClearMaterialListCache;
    }
    
    void ISelectionHotkeyTool.Flip(bool vertical) {
        if (CurrentPlacement is not ISelectionFlipHandler pl || EditorState.Map is null)
            return;

        var action = vertical ? pl.TryFlipVertical() : pl.TryFlipHorizontal();

        action?.Apply(EditorState.Map);
    }

    void ISelectionHotkeyTool.Rotate(RotationDirection dir) {
        if (CurrentPlacement is not ISelectionFlipHandler pl || EditorState.Map is null)
            return;

        pl.TryRotate(dir)?.Apply(EditorState.Map);
    }

    void ISelectionHotkeyTool.AddNode(Vector2? at) {
        if (CurrentPlacement is not EntitySelectionHandler placement) {
            return;
        }

        var entity = placement.Entity;

        // If you're currently resizing an entity with nodes, start moving the first node now.
        var resizable = (entity.ResizableX || entity.ResizableY) && !_shouldDragNodesOfResizableEntity;
        if (resizable && _draggedNodeIndex == 0 && entity.Nodes.Count > 0 ) {
            _shouldDragNodesOfResizableEntity = true;
            return;
        }

        // If you're not moving the last node, start moving the next one
        if (_draggedNodeIndex < entity.Nodes.Count - 1) {
            _draggedNodeIndex++;
            return;
        }
        
        var action = entity.Nodes.Count > 0 
            ? entity.CreateNodeSelection(entity.Nodes.Count - 1).Handler.TryAddNode(at)
            : placement.TryAddNode(at);
        
        // Add a new node
        if (action is { } res) {
            res.Item1.Apply(EditorState.Map!);
            if (RectangleGesture.Delta is not { } delta) {
                entity.InitializeNodePositions();
            } else {
                _draggedNodeIndex = entity.Nodes.Count - 1;
            }
        }
    }

    #region Imgui
    private const int PreviewSize = 32;

    public override float MaterialListElementHeight()
        => Settings.Instance.ShowPlacementIcons ? PreviewSize + ImGui.GetStyle().FramePadding.Y : base.MaterialListElementHeight();

    private static Dictionary<string, XnaWidgetDef?> MaterialPreviewCache { get; } = new();

    protected override XnaWidgetDef? GetMaterialPreview(object material) {
        if (material is string)
            return null;

        if (material is not Placement placement)
            return base.GetMaterialPreview(material);

        var keySpan = Interpolator.Temp($"pl_{placement.Name}_{placement.SID ?? ""}");
        var cacheLookup = MaterialPreviewCache.GetAlternateLookup<ReadOnlySpan<char>>();
        
        if (cacheLookup.TryGetValue(keySpan, out var value)) {
            return value;
        }

        var key = keySpan.ToString();
        XnaWidgetDef def = placement.PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.BGDecals or SelectionLayer.FGDecals }
            ? CreateWidgetForDecal(placement, key, PreviewSize)
            : CreateWidget(Map.DummyMap, placement, key);
        MaterialPreviewCache[key] = def;

        return def;
    }

    protected override XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        if (material is Placement placement && placement.IsDecal()) {
            var texture = GFX.Atlas[Decal.GetTexturePathFromPlacement(placement)];
            var maxSize = Math.Max(texture.Width, texture.Height);

            return CreateWidgetForDecal(placement, "", maxSize);
        }

        return base.CreateTooltipPreview(materialPreview, material);
    }

    private XnaWidgetDef CreateWidgetForDecal(Placement placement, string key, int size) {
        var texture = Decal.GetTexturePathFromPlacement(placement);
        var scale = size < PreviewSize ? 2 : 1;
        size *= scale;

        if (size == PreviewSize) {
            // optimised version with less locals getting captured into the lambda
            return new(key, PreviewSize, PreviewSize, () => {
                ISprite.FromTexture(new(PreviewSize / 2), texture, origin: new(.5f, .5f)).Render(SpriteRenderCtx.Default());
            });
        }

        return new(key, size, size, () => {
            (ISprite.FromTexture(new(size / 2), texture, origin: new(.5f, .5f)) with {
                Scale = new(scale),
            }).Render(SpriteRenderCtx.Default());
        });
    }

    private XnaWidgetDef CreateWidget(Map map, Placement placement, string key) {
        List<ISprite>? sprites = null;
        var cam = new Camera();
        Vector2 camOffset = default;
        var ctx = new SpriteRenderCtx(cam, default, Room.DummyRoom, false);
        var didResize = false;
        var didFixups = false;
        ISelectionHandler? selection = null;
        
        var def = new XnaWidgetDef(key, PreviewSize, PreviewSize, () => {
            if (sprites is null) {
                try {
                    var prevLogErrors = Entity.LogErrors;

                    selection = placement.PlacementHandler.CreateSelection(placement, default, Room.DummyRoom);

                    var offset = new Vector2(PreviewSize / 2, PreviewSize / 2);
                    var rect = selection.Rect;

                    Entity.LogErrors = false;
                    if (selection.TryResize(new(PreviewSize - rect.Width, PreviewSize - rect.Height)) is { } resizeAct) {
                        resizeAct.Apply(map);
                        resizeAct = null;
                        offset = default;
                        didResize = true;
                    }

                    try {
                        sprites = placement.GetWidgetSprites(selection, offset, Room.DummyRoom).ToList();
                    } catch {
                        sprites = [];
                        return;
                    } finally {
                        Entity.LogErrors = prevLogErrors;
                    }
                } catch {
                    sprites = [];
                }
                
                // clear old references to let them get GC'd
                placement = null!;
            }
            
            // fixup phase - offset sprites and/or camera to make them fix within the preview rectangle better.
            if (!didFixups) {
                selection = selection ?? throw new UnreachableException($"{nameof(selection)} is null!");
                
                // Wait with fixing up the preview offsets until all textures are ready
                if (sprites.Any(x => !x.IsLoaded))
                    return;
                
                if (!didResize) {
                    if (sprites is [Sprite onlySprite]) {
                        onlySprite.Origin = new(0.5f, 0.5f);
                        sprites[0] = onlySprite;
                    }
                }
                else if (didResize && sprites.Count > 1 && selection.Parent is Entity e && (e.ResizableX || e.ResizableY) && sprites.All(s => s is Sprite)) {
                    var spriteBounds = RectangleExt.Merge(sprites.OfType<Sprite>().Select(s => s.GetRenderRect() ?? default));

                    camOffset.X = spriteBounds.Left;
                    camOffset.Y = spriteBounds.Top;

                    if (e.ResizableX) {
                        camOffset.Y = -PreviewSize / 2f + spriteBounds.Center.Y;
                    } 
                    if (e.ResizableY) {
                        camOffset.X = -PreviewSize / 2f + spriteBounds.Center.X;
                    }
                }

                didFixups = true;
                
                selection = null;
            }

            if (didFixups) {
                cam.Goto(camOffset);
            
                foreach (var item in sprites) {
                    item.Render(ctx);
                }
            }
        }, cam, Rerender: true);
        return def;
    }

    protected override void RenderMaterialTooltipExtraInfo(object material) {
        base.RenderMaterialTooltipExtraInfo(material);

        if (material is Placement placement && placement.GetAssociatedMods() is { Count: > 0} associated) {
            ImGui.BeginTooltip();

            var currentMod = EditorState.Map?.Mod;
            ImGui.Text("Associated:");
            if (associated.Count == 1)
                ImGui.SameLine();
            foreach (var mod in associated) {
                var displayName = ModMeta.ModNameToDisplayName(mod);
                if (currentMod is { } && !currentMod.DependencyMet(mod)) {
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.ToNumVec4());
                    ImGui.TextWrapped(displayName);
                    ImGui.PopStyleColor(1);
                } else {
                    ImGui.TextWrapped(displayName);
                }
            }

            if (placement.GetDefiningMod() is { } defining && (associated.Count != 1 || associated[0] != defining.Name)) {
                ImGui.BeginDisabled();
                ImGui.Text("Defined by:");
                ImGui.SameLine();
                ImGui.TextWrapped(defining.DisplayName);
                ImGui.EndDisabled();
            }
            ImGui.EndTooltip();
        }
    }

    public override object GetGroupKeyForMaterial(object material)
        => material is Placement { SID: not null } pl && pl.SID != EntityRegistry.FGDecalSID && pl.SID != EntityRegistry.BGDecalSID ? pl.SID : material;

    protected override bool RenderMaterialListElement(object material, string name) {
        if (material is Placement placement && !placement.AreAssociatedModsADependencyOfCurrentMap()) {
            ImGuiManager.PushNullStyle();
        }

        var ret = base.RenderMaterialListElement(material, name);
        ImGuiManager.PopNullStyle();

        if (Layer == EditorLayers.Prefabs) {
            if (ImGui.BeginPopupContextItem(name, ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Remove")) {
                    PrefabHelper.Remove(name);
                }

                ImGui.EndPopup();
            }
        }

        return ret;
    }

    public override void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        if (Layer == EditorLayers.Prefabs && !(GetMaterials(Layer)?.Any() ?? true)) {
            ImGui.TextWrapped("rysy.tools.placement.noPrefabs".TranslateFormatted(Settings.Instance.GetHotkey(SelectionTool.CreatePrefabKeybindName)));
            showSearchBar = true;
        } else {
            base.RenderMaterialList(size, out showSearchBar);
        }
    }

    #endregion
}
