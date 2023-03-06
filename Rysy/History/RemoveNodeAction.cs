namespace Rysy.History;
public record class RemoveNodeAction(Node Node, Entity Entity) : IHistoryAction {
    private int Index;

    public bool Apply() {

        if (Entity.Nodes is { } nodes && (Index = nodes.IndexOf(Node)) != -1) {
            nodes.RemoveAt(Index);

#warning Handle minimum nodes!

            Entity.ClearRoomRenderCache();
            return true;
        }

        return false;
    }

    public void Undo() {
        Entity.Nodes?.Insert(Index, Node);
        Entity.ClearRoomRenderCache();
    }
}
