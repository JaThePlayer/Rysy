using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Selections;
using Rysy.Signals;
using System.Diagnostics;

namespace Rysy.Tools;
public class PlacementTool : Tool, ISelectionHotkeyTool, ISignalListener<PrefabsChanged> {
    public ISelectionHandler? CurrentPlacement { get; private set; }
    private object? _currentPlacementSourceMaterial;

    public SelectRectangleGesture RectangleGesture;

    private bool _pickNextFrame;

    /// <summary>
    /// Position at which the GetPreviewSprites method will be called, regardless of the RectangleGesture's location. 
    /// </summary>
    private Vector2? _anchorPos;

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

    public override IReadOnlyList<IEditorLayer> ValidLayers =>
        ToolHandler.ComponentRegistry.GetAll<IPlacementEditorLayer>();

    public override string? SerializeMaterial(IEditorLayer layer, object? material) {
        return material switch {
            Placement pl => pl.ToJson(),
            _ => null
        };
    }

    public override object? DeserializeMaterial(IEditorLayer layer, string serializableMaterial) {
        if (JsonExtensions.TryDeserialize(serializableMaterial, out Placement? res)) {
            var mats = GetMaterials(layer);
            return mats?.OfType<Placement>().FirstOrDefault(x => x == res) ?? res;
        }

        return null;
    }

    private static Placement? PlacementFromString(string str, IEditorLayer layer) {
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
        
        ClearPreviewCache();
    }

    protected internal override void OnThemeChanged(Theme theme) {
        base.OnThemeChanged(theme);
        
        ClearPreviewCache();
    }

    private void ClearPreviewCache() {
        var cache = MaterialPreviewCache;
        MaterialPreviewCache.Clear();
        RysyState.OnEndOfThisFrame += () => {
            foreach (var (k, v) in cache) {
                ImGuiManager.DisposeXnaWidget(k);
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
        
        if (_pickNextFrame) {
            _pickNextFrame = false;
            if (GetPlacementUnderCursor(GetMousePos(camera, room, precise: true), room, EditorLayers.ToolLayerToEnum(Layer)) is { } underCursor) {
                Material = underCursor;
                ToolHandler.PushRecentMaterial(Material);
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
                History.ApplyNewAction(place.PlacementHandler.Place(EditorState, placement, room!));
                _anchorPos = null;
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
                _anchorPos ??= e.Pos;
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
            CurrentPlacement = handler.CreateSelection(EditorState, place, GetMousePos(camera, currentRoom).ToVector2(), currentRoom!);
            _currentPlacementSourceMaterial = Material;
            if (CurrentPlacement is EntitySelectionHandler entityHandler) {
                entityHandler.Entity.InitializeNodePositions();
            }
        }
    }

    public void OnMiddleClick() {
        _pickNextFrame = true;
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
            var pos = _anchorPos ?? (RectangleGesture.CurrentRectangle is { } rect ? rect.Location.ToVector2() : mouse.ToVector2());
            var ctx = SpriteRenderCtx.Default();
            var offset = placement.PlacementHandler.GetPreviewSpritesOffset(EditorState, selection, pos, room!);
            SpriteBatchState prevState = default;
            if (offset is { }) {
                prevState = Gfx.EndBatch()!.Value;
                Gfx.BeginBatch(prevState with {
                    TransformMatrix = prevState.TransformMatrix * Matrix.CreateTranslation(offset.Value.X * camera.Scale, offset.Value.Y * camera.Scale, 0f)
                });
            }
            
            foreach (var item in placement.GetPreviewSprites(EditorState, selection, pos, room!)) {
                if (item is Sprite spr) {
                    spr.RenderWithColor(ctx, spr.Color * 0.4f);
                } else {
                    item.WithMultipliedAlpha(0.4f).Render(ctx);
                }
            }

            if (offset is { }) {
                Gfx.EndBatch();
                Gfx.BeginBatch(prevState);
            }
        }

        if (!ImGuiManager.WantCaptureMouse && !ImGui.IsAnyItemHovered()) {
            var mousePos = GetMousePos(camera, room);

            var selectionsUnderCursor = room?.GetSelectionsInRect(new Rectangle(mousePos.X, mousePos.Y, 1, 1), EditorLayers.ToolLayerToEnum(Layer));
            
            SelectionTool.HandleHoveredSelections(this, room, selectionsUnderCursor, selected: null, Input);
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
            Gfx.EndBatch();
            Gfx.BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix));
            Render(camera, null);
        }
    }

    public override void CancelInteraction() {
        base.CancelInteraction();

        if (CurrentPlacement is { } pl && 
            (pl.Layer != Layer || _currentPlacementSourceMaterial != Material)) {
            CurrentPlacement = null;
            _currentPlacementSourceMaterial = null;
        }
        _pickNextFrame = false;
        _anchorPos = null;
        ResetDragState();
    }

    public override void Init() {
        base.Init();

        ScopedComponentRegistry.Add(RectangleGesture = new SelectRectangleGesture(Input));
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

    void ISelectionHotkeyTool.SwapDecalLayer() {
        if (CurrentPlacement is not EntitySelectionHandler { Entity: Decal decal } placement) {
            return;
        }

        Layer = decal.Fg ? EditorLayers.BgDecals : EditorLayers.FgDecals;
        Material = decal.ToPlacement().WithSid(decal.Fg ? EntityRegistry.BgDecalSid : EntityRegistry.FgDecalSid);
    }

    #region Imgui
    public override float MaterialListElementHeight()
        => Settings.Instance.ShowPlacementIcons ? PreviewSize + ImGui.GetStyle().FramePadding.Y : base.MaterialListElementHeight();

    private static Dictionary<string, XnaWidgetDef?> MaterialPreviewCache { get; } = new();

    internal override XnaWidgetDef? GetMaterialPreview(IEditorLayer layer, object material) {
        if (material is string)
            return null;

        if (material is not Placement placement)
            return base.GetMaterialPreview(layer, material);
        
       // MaterialPreviewCache.Clear();

        var keySpan = Interpolator.Temp($"pl_{placement.Name}_{placement.Sid ?? ""}_{placement.ValueOverrides.ContentsHashCode()}");
        var cacheLookup = MaterialPreviewCache.GetAlternateLookup<ReadOnlySpan<char>>();
        
        if (cacheLookup.TryGetValue(keySpan, out var value)) {
            return value;
        }

        var key = keySpan.ToString();
        XnaWidgetDef def = placement.PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.BgDecals or SelectionLayer.FgDecals }
            ? CreateWidgetForDecal(placement, key, PreviewSize)
            : CreateWidget(Map.DummyMap, placement, key);
        MaterialPreviewCache[key] = def;

        return def;
    }

    protected override XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        if (material is Placement placement && placement.IsDecal()) {
            var texture = Gfx.Atlas[Decal.GetTexturePathFromPlacement(placement)];
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

                    selection = placement.PlacementHandler.CreateSelection(EditorState, placement, default, Room.DummyRoom);

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
                        sprites = placement.GetWidgetSprites(EditorState, selection, offset, Room.DummyRoom).ToList();
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
                sprites ??= [];
            }
            
            // fixup phase - offset sprites and/or camera to make them fix within the preview rectangle better.
            if (!didFixups) {
                // Wait with fixing up the preview offsets until all textures are ready
                if (sprites.Any(x => !x.IsLoaded))
                    return;
                
                if (!didResize) {
                    if (sprites is [Sprite onlySprite]) {
                        //onlySprite.Origin = new(0.5f, 0.5f);
                        onlySprite.DrawOffset = onlySprite.Texture.DrawOffset;
                        sprites[0] = onlySprite;
                    }
                }
                else if (didResize && sprites.Count > 1 && selection?.Parent is Entity e && (e.ResizableX || e.ResizableY) && sprites.All(s => s is Sprite)) {
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

    protected override bool RenderMaterialListElement(IEditorLayer layer, object material, Searchable searchable) {
        var ret = base.RenderMaterialListElement(layer, material, searchable);

        if (Layer is PrefabLayer prefabLayer) {
            if (ImGui.BeginPopupContextItem(searchable.TextWithMods, ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Remove")) {
                    prefabLayer.PrefabHelper.Remove(searchable.Text);
                }

                ImGui.EndPopup();
            }
        }

        return ret;
    }

    public override void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        if (Layer is PrefabLayer && !(GetMaterials(Layer)?.Any() ?? true)) {
            ImGui.TextWrapped("rysy.tools.placement.noPrefabs".TranslateFormatted(Settings.Instance.GetHotkey(SelectionTool.CreatePrefabKeybindName)));
            showSearchBar = true;
        } else {
            base.RenderMaterialList(size, out showSearchBar);
        }
    }

    #endregion

    public void OnSignal(PrefabsChanged signal) {
        ClearMaterialListCache();
    }
}
