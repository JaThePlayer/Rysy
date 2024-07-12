namespace Rysy.History;
public record class RemoveNodeAction(Node Node, Entity Entity) : IHistoryAction {
    private int Index;

    public bool Apply(Map map) {
        if (Entity.Nodes is { } nodes && (Index = nodes.IndexOf(Node)) != -1) {
            nodes.RemoveAt(Index);
            Entity.ClearRoomRenderCache();
            return true;
        }

        return false;
    }

    public void Undo(Map map) {
        Entity.Nodes?.Insert(Index, Node);
        Entity.ClearRoomRenderCache();
    }
}
