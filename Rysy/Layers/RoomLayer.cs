using Rysy.Selections;

namespace Rysy.Layers;

public class RoomLayer : EditorLayer {
    public override string Name => "Rooms";
    public override SelectionLayer SelectionLayer => SelectionLayer.Rooms;
    
    public override IEnumerable<Placement> GetMaterials() {
        return [ new Placement() {
            ValueOverrides = new() {
                ["width"] = 40 * 8,
                ["height"] = 23 * 8,
            },
            PlacementHandler = new RoomPlacementHandler(),
            Name = "New Room",
        }];
    }
}