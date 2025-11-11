namespace Rysy.History;
public record class RemoveNodeAction(Node Node, Entity Entity) : IHistoryAction {
    private int _index;

    public bool Apply(Map map) {
        if (Entity.Nodes is { } nodes && (_index = nodes.IndexOf(Node)) != -1) {
            nodes.RemoveAt(_index);
            Entity.ClearRoomRenderCache();
            return true;
        }

        return false;
    }

    public void Undo(Map map) {
        Entity.Nodes?.Insert(_index, Node);
        Entity.ClearRoomRenderCache();
    }
}
