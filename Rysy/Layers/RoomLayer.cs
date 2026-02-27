using Rysy.Selections;

namespace Rysy.Layers;

public class RoomLayer : EditorLayer, IPlacementEditorLayer, ISelectionEditorLayer {
    public override string Name => "Rooms";
    public override SelectionLayer SelectionLayer => SelectionLayer.Rooms;

    public override string MaterialLangPrefix => "rysy.roomPlacements";
    
    public override IEnumerable<Placement> GetMaterials() {
        return [ 
            new Placement() {
                ValueOverrides = new() {
                    ["width"] = 40 * 8,
                    ["height"] = 23 * 8,
                    ["hasPlayer"] = true,
                },
                PlacementHandler = new RoomPlacementHandler(),
                Sid = "room",
                Name = "default"
            },
            new Placement() {
                ValueOverrides = new() {
                    ["width"] = 8,
                    ["height"] = 8,
                    ["hasPlayer"] = false,
                },
                PlacementHandler = new RoomPlacementHandler(),
                Sid = "fillerRoom",
                Name = "filler",
            }
        ];
    }

    public List<Selection> GetSelectionsInRect(Map map, Room? unused, Rectangle? rect) {
        var list = new List<Selection>();
        foreach (var room in map.Rooms) {
            var handler = room.GetSelectionHandler();

            if (rect is null || handler.IsWithinRectangle(rect.Value)) {
                list.Add(new Selection(handler));
            }
        }

        return list;
    }

    public override bool SupportsPreciseMoveMode => false;

    public override int? ForcedGridSize => 8;
}