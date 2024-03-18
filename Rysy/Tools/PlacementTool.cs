using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Selections;

namespace Rysy.Tools;
public class PlacementTool : Tool {
    public ISelectionHandler? CurrentPlacement;

    public SelectRectangleGesture RectangleGesture;

    private bool PickNextFrame;

    /// <summary>
    /// Position at which the GetPreviewSprites method will be called, regardless of the RectangleGesture's location. 
    /// </summary>
    private Vector2? AnchorPos;

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("tools.pick", "mousemiddle", OnMiddleClick);
        handler.AddHotkeyFromSettings("selection.flipHorizontal", "h", () => Flip(false));
        handler.AddHotkeyFromSettings("selection.flipVertical", "v", () => Flip(true));
        handler.AddHotkeyFromSettings("selection.rotateRight", "r", () => Rotate(RotationDirection.Right));
        handler.AddHotkeyFromSettings("selection.rotateLeft", "l", () => Rotate(RotationDirection.Left));
    }

    public override string Name => "placement";

    public override string PersistenceGroup => "placement";

    private static List<EditorLayer> _validLayers = new() {
        EditorLayers.Entities,
        EditorLayers.Triggers,
        EditorLayers.FgDecals,
        EditorLayers.BgDecals,
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
            
            var name = EditorLayers.IsDecalLayer(layer) ? pl.Name : pl.Name.TranslateOrHumanize($@"{(pl.IsTrigger() ? "triggers" : "entities")}.{pl.SID}.placements.name");

            var associated = pl.GetAssociatedMods();
            if (associated is { Count: > 0}) {
                return $"{name} [{string.Join(',', associated)}]";
            }

            return $"{name} [Vanilla]";
        }

        return material switch {
            string s => s,
            _ => material?.ToString() ?? "null",
        };
    }

    public override string? GetMaterialTooltip(EditorLayer layer, object material) {
        return material switch {
            Placement pl => pl.Tooltip ?? pl.Name.TranslateOrNull($@"{(pl.IsTrigger() ? "triggers" : "entities")}.{pl.SID}.placements.description"),
            _ => null,
        };
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
        RysyEngine.OnEndOfThisFrame += () => {
            foreach (var (k, v) in cache) {
                ImGuiManager.DisposeXnaWidget(k.ToString());
            }
            cache.Clear();
        };
    }

    public override void Update(Camera camera, Room room) {
        if (PickNextFrame) {
            PickNextFrame = false;
            if (GetPlacementUnderCursor(GetMousePos(camera, room, precise: true), room, EditorLayers.ToolLayerToEnum(Layer)) is { } underCursor) {
                Material = underCursor;
            }
            CurrentPlacement = null;
        }

        if (CurrentPlacement is not { } selection) {
            CreatePlacementFromMaterial(camera, room);
            return;
        }

        if (Material is Placement place) {
            if (RectangleGesture.Update((p) => GetMousePos(camera, room, position: p.ToVector2())) is { } rect) {
                History.ApplyNewAction(place.PlacementHandler.Place(selection, room));
                AnchorPos = null;
            }

            HandleMove(camera, room, selection);
        }
    }

    private void HandleMove(Camera camera, Room room, ISelectionHandler selection) {
        if (RectangleGesture.Delta is not { } delta) 
            return;
        
        var offset = delta.Location.ToVector2();
        var resize = delta.Size();
        
        if (offset == Vector2.Zero && resize == Point.Zero)
            return;

        // handle noded entity resizing being different
        // TODO: refactor, maybe into a ICustomMoveHandler
        if (selection is EntitySelectionHandler entityHandler) {
            var e = entityHandler.Entity;
            var resizableX = e.ResizableX;
            var resizableY = e.ResizableY;

            if (!resizableX && !resizableY && e.Nodes is [var onlyNode]) {
                new MoveNodeAction(onlyNode, e, GetMousePos(camera, room).ToVector2() - onlyNode).Apply();
                AnchorPos ??= e.Pos;
                return;
            }

            if (offset.X != 0 || offset.Y != 0) {
                selection.MoveBy(offset).Apply();
            }

            if ((resize.X != 0 || resize.Y != 0) && selection.TryResize(resize) is { } resizeAction) {
                resizeAction.Apply();
                e.InitializeNodePositions();
            }

            return;
        }

        if (offset.X != 0 || offset.Y != 0) {
            selection.MoveBy(offset).Apply();
        }

        if (resize.X != 0 || resize.Y != 0) {
            selection.TryResize(resize)?.Apply();
        }
    }

    private void CreatePlacementFromMaterial(Camera camera, Room currentRoom) {
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
            CurrentPlacement = handler.CreateSelection(place, GetMousePos(camera, currentRoom).ToVector2(), currentRoom);
        }
    }

    public void OnMiddleClick() {
        PickNextFrame = true;
    }

    internal static Placement? GetPlacementUnderCursor(Point mouse, Room currentRoom, SelectionLayer layer) {
        var selections = currentRoom.GetSelectionsInRect(new(mouse.X, mouse.Y, 1, 1), layer);
        if (selections.Count == 0)
            return null;

        if (selections[0].Handler.Parent is { } parent && Placement.TryCreateFromObject(parent) is { } placement)
            return placement;

        return null;
    }

    public override void Render(Camera camera, Room room) {
        var mouse = GetMousePos(camera, room);

        if (Material is Placement placement && CurrentPlacement is { } selection) {
            var pos = AnchorPos ?? (RectangleGesture.CurrentRectangle is { } rect ? rect.Location.ToVector2() : mouse.ToVector2());
            var ctx = SpriteRenderCtx.Default();
            
            foreach (var item in placement.GetPreviewSprites(selection, pos, room)) {
                if (item is Sprite spr) {
                    // don't box
                    spr.WithMultipliedAlpha(0.4f).Render(ctx);
                } else {
                    item.WithMultipliedAlpha(0.4f).Render(ctx);
                }
            }
        }

        if (!ImGui.GetIO().WantCaptureMouse && !ImGui.IsAnyItemHovered()) {
            var mousePos = GetMousePos(camera, room);

            SelectionTool.HandleHoveredSelections(room, new Rectangle(mousePos.X, mousePos.Y, 1, 1),
                EditorLayers.ToolLayerToEnum(Layer), selected: null, Input, render: false
            );
        }
    }

    private Point GetMousePos(Camera camera, Room currentRoom, bool? precise = null, Vector2? position = null) {
        precise ??= Input.Keyboard.Ctrl();

        var pos = currentRoom.WorldToRoomPos(camera, position ?? Input.Mouse.Pos.ToVector2());

        if (!precise.Value) {
            pos = pos.Snap(8);
        }

        return pos.ToPoint();
    }

    public override void RenderOverlay() {
    }

    public override void CancelInteraction() {
        base.CancelInteraction();

        CurrentPlacement = null;
        PickNextFrame = false;
        AnchorPos = null;
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
    
    private void Flip(bool vertical) {
        if (CurrentPlacement is not ISelectionFlipHandler pl)
            return;

        var action = vertical ? pl.TryFlipVertical() : pl.TryFlipHorizontal();

        action?.Apply();
    }

    private void Rotate(RotationDirection dir) {
        if (CurrentPlacement is not ISelectionFlipHandler pl)
            return;

        pl.TryRotate(dir)?.Apply();
    }

    #region Imgui
    private const int PreviewSize = 32;

    public override float MaterialListElementHeight()
        => Settings.Instance.ShowPlacementIcons ? PreviewSize + ImGui.GetStyle().FramePadding.Y : base.MaterialListElementHeight();

    private static Dictionary<StringRef, XnaWidgetDef?> MaterialPreviewCache { get; } = new();

    protected override XnaWidgetDef? GetMaterialPreview(object material) {
        if (material is string)
            return null;

        if (material is not Placement placement)
            return base.GetMaterialPreview(material);

        var keySpan = Interpolator.Shared.Interpolate($"pl_{placement.Name}_{placement.SID ?? ""}");
        if (MaterialPreviewCache.TryGetValue(StringRef.FromSharedBuffer(Interpolator.Shared.Buffer, keySpan.Length), out var value)) {
            return value;
        }

        if (EditorState.Map is not { })
            return null;

        var key = keySpan.ToString();
        XnaWidgetDef def = placement.PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.BGDecals or SelectionLayer.FGDecals }
            ? CreateWidgetForDecal(placement, key, PreviewSize)
            : CreateWidget(placement, key);
        MaterialPreviewCache[StringRef.FromString(key)] = def;

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

    private XnaWidgetDef CreateWidget(Placement placement, string key) {
        List<ISprite>? sprites = null;
        var def = new XnaWidgetDef(key, PreviewSize, PreviewSize, () => {
            if (sprites is null) {
                var prevLogErrors = Entity.LogErrors;

                var r = Room.DummyRoom;
                var s = placement.PlacementHandler.CreateSelection(placement, default, r);

                var offset = new Vector2(PreviewSize / 2, PreviewSize / 2);
                var rect = s.Rect;
                var didResize = false;
                
                Entity.LogErrors = false;
                if (s.TryResize(new(PreviewSize - rect.Width, PreviewSize - rect.Height)) is { } resizeAct) {
                    resizeAct.Apply();
                    resizeAct = null;
                    offset = default;
                    didResize = true;
                }

                try {
                    sprites = placement.GetPreviewSprites(s, offset, r).ToList();
                } catch {
                    sprites = new();
                    return;
                } finally {
                    Entity.LogErrors = prevLogErrors;
                }

                if (!didResize) {
                    /*
                    var spriteBounds = ISprite.GetBounds(sprites);
                    offset = (spriteBounds.Size.ToVector2()) / 2;
                    
                    sprites = placement.GetPreviewSprites(s, offset, r).ToList();*/
                    if (sprites is [Sprite onlySprite]) {
                        offset *= onlySprite.Origin;
                        onlySprite.Origin = default;
                    }
                }
                
                // clear old references to let them get GC'd
                r = null;
                s = null;
                placement = null!;
            }

            var ctx = SpriteRenderCtx.Default();
            foreach (var item in sprites) {
                item.Render(ctx);
            }
        }, Rerender: true);
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
                if (currentMod is { } && !currentMod.DependencyMet(mod)) {
                    ImGui.TextColored(Color.Red.ToNumVec4(), mod);
                } else {
                    ImGui.Text(mod);
                }
            }

            if (placement.GetDefiningMod() is { } defining)
                ImGui.TextDisabled(Interpolator.Shared.Interpolate($"Defined by: {defining.Name}"));
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
