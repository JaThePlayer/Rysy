using Rysy.Graphics;
using Rysy.Gui.Elements;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Triggers;

namespace Rysy.Tools;
public class PlacementTool : Tool {
    public override string Name => "Placement";

    public override string PersistenceGroup => "placement";

    private static List<string> _validLayers = new() { 
        LayerNames.ENTITIES, 
        LayerNames.TRIGGERS,
        LayerNames.FG_DECALS,
    };
    public override List<string> ValidLayers => _validLayers;

    public override IEnumerable<object>? GetMaterials(string layer) {
        return layer switch {
            LayerNames.ENTITIES => EntityRegistry.EntityPlacements,
            LayerNames.TRIGGERS => EntityRegistry.TriggerPlacements,
            LayerNames.FG_DECALS => GFX.ValidDecalPaths,
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

    public override void Update(Camera camera, Room currentRoom) {

        if (Material is Placement placement) {
            if (Input.Mouse.Left.Clicked()) {
                Input.Mouse.ConsumeLeft();
                var mouse = GetMousePos(camera, currentRoom);

                History.ApplyNewAction(new AddEntityAction(CreateEntity(currentRoom, mouse, placement, assignID: true), currentRoom));
            }
        }

#warning TEMP
        if (Input.Mouse.Right.Clicked()) {
            Entity? ent = GetEntityUnderCursor(camera, currentRoom);
            if (ent is { }) {
                Input.Mouse.ConsumeRight();
                RysyEngine.Scene.AddWindow(new EntityPropertyWindow(ent));
            }
        }

        if (Input.Mouse.Middle.Clicked()) {
            Entity? ent = GetEntityUnderCursor(camera, currentRoom);
            if (ent is { }) {
                Input.Mouse.ConsumeMiddle();
                Material = new Placement(ent.EntityData.Name) {
                    SID = ent.EntityData.Name,
                    IsTrigger = ent is Trigger,
                    ValueOverrides = ent.EntityData.Inner,
                };
            }
        }
    }

    private static Entity? GetEntityUnderCursor(Camera camera, Room currentRoom) {
        var mouse = GetMousePos(camera, currentRoom, precise: true);

        var ent = currentRoom.Entities.FirstOrDefault(e => e.GetSelection().Check(mouse.ToVector2(), out int node));
        return ent;
    }

    public override void Render(Camera camera, Room currentRoom) {
        var mouse = GetMousePos(camera, currentRoom);

        if (Material is Placement placement) {
            foreach (var item in CreateEntity(currentRoom, mouse, placement, assignID: false).GetSprites()) {
                item.Render();
            }
        }
    }

    private static Entity CreateEntity(Room currentRoom, Point pos, Placement placement, bool assignID) {
        return EntityRegistry.Create(placement, pos.ToVector2(), currentRoom, assignID);
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
