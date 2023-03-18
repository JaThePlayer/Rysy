using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools;
public class PlacementTool : Tool {
    private bool PickNextFrame;

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("tools.pick", "mousemiddle", OnMiddleClick);
    }

    public override string Name => "Placement";

    public override string PersistenceGroup => "placement";

    private static List<string> _validLayers = new() {
        LayerNames.ENTITIES,
        LayerNames.TRIGGERS,
        LayerNames.FG_DECALS,
        LayerNames.BG_DECALS,
    };
    public override List<string> ValidLayers => _validLayers;

    public override IEnumerable<object>? GetMaterials(string layer) {
        return layer switch {
            LayerNames.ENTITIES => EntityRegistry.EntityPlacements,
            LayerNames.TRIGGERS => EntityRegistry.TriggerPlacements,
            LayerNames.FG_DECALS => GFX.ValidDecalPaths,
            LayerNames.BG_DECALS => GFX.ValidDecalPaths,
            _ => throw new NotImplementedException(layer)
        };
    }

    public override string GetMaterialDisplayName(string layer, object material) {
        return material switch {
            Placement pl => pl.Name,
            string s => s,
            _ => material?.ToString() ?? "null",
        };
    }

    public override string? GetMaterialTooltip(string layer, object material) {
        return material switch {
            Placement pl => pl.Tooltip,
            _ => null,
        };
    }

    private Placement? PlacementFromString(string str) {
        return Layer switch {
            LayerNames.FG_DECALS => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            LayerNames.BG_DECALS => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            _ => null,
        };
    }

    public override void Update(Camera camera, Room currentRoom) {
        if (Material is string strPlacement) {
            Material = PlacementFromString(strPlacement);
        }
        

        if (Material is Placement placement) {
            if (Input.Mouse.Left.Clicked()) {
                Input.Mouse.ConsumeLeft();
                var mouse = GetMousePos(camera, currentRoom);

                History.ApplyNewAction(placement.Place(mouse.ToVector2(), currentRoom));
            }
        }

#warning TEMP
        /*
                if (Input.Mouse.Right.Clicked()) {
                    Entity? ent = GetPlacementUnderCursor(camera, currentRoom);
                    if (ent is { }) {
                        Input.Mouse.ConsumeRight();
                        RysyEngine.Scene.AddWindow(new EntityPropertyWindow(ent));
                    }
                }*/

        if (PickNextFrame) {
            PickNextFrame = false;
            if (GetPlacementUnderCursor(camera, currentRoom) is { } place) {
                Material = place;
            }
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

        if (Material is Placement placement) {
            foreach (var item in placement.GetPreviewSprites(mouse.ToVector2(), currentRoom)) {
                item.MultiplyAlphaBy(0.4f);
                item.Render();
            }
        }
    }

    private static Point GetMousePos(Camera camera, Room currentRoom, bool? precise = null) {
        precise ??= Input.Keyboard.Ctrl();

        var pos = currentRoom.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2());

        if (!precise.Value) {
            pos = pos.Snap(8);
        }

        return pos.ToPoint();
    }

    public override void RenderOverlay() {
    }

}
