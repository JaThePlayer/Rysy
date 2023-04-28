namespace Rysy.History;

public record class AddNodeAction(Entity Entity, Node Node, int NodeIdx) : IHistoryAction {
    public bool Apply() {
        var node = Node;
        var nodes = Entity.Nodes;

        nodes.Insert(NodeIdx, node);

        Entity.ClearRoomRenderCache();
        Entity.OnChanged();

        return true;
    }

    public void Undo() {
        var nodes = Entity.Nodes;

        nodes.Remove(Node);

        Entity.ClearRoomRenderCache();
        Entity.OnChanged();
    }
}
