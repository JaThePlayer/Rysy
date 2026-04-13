using Rysy.Selections;

namespace Rysy.Layers;

public sealed class AllLayer : EditorLayer, ISelectionEditorLayer {
    public override string Name => "All";

    public override SelectionLayer SelectionLayer => SelectionLayer.All;

    public override IReadOnlyList<object> GetContents(Room room) => [];

    public override IEnumerable<Placement> GetMaterials() {
        return [];
    }

    public List<Selection> GetSelectionsInRect(Map map, Room? room, Rectangle? rect) {
        if (room is null)
            return [];

        return room.GetSelectionsInRect(rect, SelectionLayer);
    }
}