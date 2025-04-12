using Rysy.Selections;

namespace Rysy.Layers;

public class RoomLayer() : EditorLayer("Rooms") {
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
                SID = "room",
                Name = "default"
            },
            new Placement() {
                ValueOverrides = new() {
                    ["width"] = 8,
                    ["height"] = 8,
                    ["hasPlayer"] = false,
                },
                PlacementHandler = new RoomPlacementHandler(),
                SID = "fillerRoom",
                Name = "filler",
            }
        ];
    }

    public override bool SupportsPreciseMoveMode => false;
}