using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;
using Rysy.Selections;

namespace Rysy.Tools;
public class PlacementTool : Tool {
    public ISelectionHandler? CurrentPlacement;

    public SelectRectangleGesture RectangleGesture;

    private bool PickNextFrame;

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("tools.pick", "mousemiddle", OnMiddleClick);
    }

    public override string Name => "placement";

    public override string PersistenceGroup => "placement";

    private static List<string> _validLayers = new() {
        LayerNames.ENTITIES,
        LayerNames.TRIGGERS,
        LayerNames.FG_DECALS,
        LayerNames.BG_DECALS,
        LayerNames.PREFABS,
    };
    public override List<string> ValidLayers => _validLayers;

    public override IEnumerable<object>? GetMaterials(string layer) {
        return layer switch {
            LayerNames.ENTITIES => EntityRegistry.EntityPlacements,
            LayerNames.TRIGGERS => EntityRegistry.TriggerPlacements,
            LayerNames.FG_DECALS => GFX.ValidDecalPaths,
            LayerNames.BG_DECALS => GFX.ValidDecalPaths,
            LayerNames.PREFABS => PrefabHelper.CurrentPrefabs.Select(s => s.Key),
            null => null,
            _ => throw new NotImplementedException(layer)
        };
    }

    public override string GetMaterialDisplayName(string layer, object material) {
        if (material is Placement pl) {
            var name = pl.Name.TranslateOrHumanize($@"{(pl.IsTrigger() ? "triggers" : "entities")}.{pl.SID}.placements.name");
            return pl.GetMod() is { } mod ? $"{name} [{mod.Name}]" : name;
        }

        return material switch {
            string s => s,
            _ => material?.ToString() ?? "null",
        };
    }

    public override string? GetMaterialTooltip(string layer, object material) {
        return material switch {
            Placement pl => pl.Tooltip ?? pl.Name.TranslateOrNull($@"{(pl.IsTrigger() ? "triggers" : "entities")}.{pl.SID}.placements.description"),
            _ => null,
        };
    }

    private Placement? PlacementFromString(string str) {
        return Layer switch {
            LayerNames.FG_DECALS => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            LayerNames.BG_DECALS => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            LayerNames.PREFABS => PrefabHelper.PlacementFromName(str),
            _ => null,
        };
    }

    public override void Update(Camera camera, Room currentRoom) {
        if (PickNextFrame) {
            PickNextFrame = false;
            if (GetPlacementUnderCursor(camera, currentRoom) is { } underCursor) {
                Material = underCursor;
            }
            CurrentPlacement = null;
        }

        if (CurrentPlacement is not { } selection) {
            CreatePlacementFromMaterial(camera, currentRoom);
            return;
        }

        if (Material is Placement place) {
            //Input.Mouse.ConsumeLeft();

            //History.ApplyNewAction(place.PlacementHandler.Place(selection, currentRoom));
            if (RectangleGesture.Update((p) => GetMousePos(camera, currentRoom, position: p.ToVector2())) is { } rect) {
                //Console.WriteLine(rect);
                History.ApplyNewAction(place.PlacementHandler.Place(selection, currentRoom));
            }

            if (RectangleGesture.Delta is { } delta) {
                var offset = delta.Location.ToVector2();
                var resize = delta.Size;

                if (offset.X != 0 || offset.Y != 0) {
                    selection.MoveBy(offset).Apply();
                }

                if (resize.X != 0 || resize.Y != 0) {
                    selection.TryResize(resize)?.Apply();
                }
            }
        }
    }

    private void CreatePlacementFromMaterial(Camera camera, Room currentRoom) {
        if (Material is string strPlacement) {
            Material = PlacementFromString(strPlacement);
        }


        if (Material is Placement place) {
            var handler = place.PlacementHandler;
            CurrentPlacement = handler.CreateSelection(place, GetMousePos(camera, currentRoom).ToVector2(), currentRoom);
        }
    }

    public void OnMiddleClick() {
        PickNextFrame = true;
    }

    private Placement? GetPlacementUnderCursor(Camera camera, Room currentRoom) {
        var mouse = GetMousePos(camera, currentRoom, precise: true);
        var selections = currentRoom.GetSelectionsInRect(new(mouse, new(1, 1)), LayerNames.ToolLayerToEnum(Layer));

        if (selections.FirstOrDefault()?.Handler.Parent is { } parent && Placement.TryCreateFromObject(parent) is { } placement)
            return placement;

        return null;
    }

    public override void Render(Camera camera, Room currentRoom) {
        var mouse = GetMousePos(camera, currentRoom);

        if (Material is Placement placement && CurrentPlacement is { } selection) {
            foreach (var item in placement.GetPreviewSprites(selection, RectangleGesture.CurrentRectangle is { } rect ? rect.Location.ToVector2() : mouse.ToVector2(), currentRoom)) {
                item.WithMultipliedAlpha(0.4f).Render();
            }
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
    }

    public override void Init() {
        base.Init();

        PrefabHelper.CurrentPrefabs.OnChanged += ClearMaterialListCache;

        RectangleGesture = new(Input);
    }

    protected override void RenderMaterialListElement(object material, string name) {
        base.RenderMaterialListElement(material, name);

        if (Layer == LayerNames.PREFABS) {
            if (ImGui.BeginPopupContextItem(name, ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Remove")) {
                    PrefabHelper.Remove(name);
                }

                ImGui.EndPopup();
            }
        }
    }
}
