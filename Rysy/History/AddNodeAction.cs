namespace Rysy.History;

public record class AddNodeAction(Entity Entity, Node Node, int NodeIdx) : IHistoryAction {
    public bool Apply() {
        var node = Node;

        if (Entity.Nodes is not { } nodes) {
            nodes = Entity.EntityData.Nodes = new List<Node>();
        }

        nodes.Insert(NodeIdx, node);

        Entity.ClearRoomRenderCache();

        return true;
    }

    public void Undo() {
        var nodes = Entity.Nodes;

        nodes!.Remove(Node);

        Entity.ClearRoomRenderCache();
    }
}
