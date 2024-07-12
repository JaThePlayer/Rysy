namespace Rysy.History;

public record class AddNodeAction(EntityRef Entity, Node Node, int NodeIdx) : IHistoryAction {
    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        var node = Node;
        var nodes = entity.Nodes;

        nodes.Insert(NodeIdx, node);
        entity.ClearRoomRenderCache();
        
        return true;
    }

    public void Undo(Map map) {
        var entity = Entity.Resolve(map);
        var nodes = entity.Nodes;

        nodes.Remove(Node);
        entity.ClearRoomRenderCache();
    }
}
