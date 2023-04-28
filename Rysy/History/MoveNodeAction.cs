namespace Rysy.History;

public record class MoveNodeAction(Node Node, Entity Entity, Vector2 By) : IHistoryAction {
    public bool Apply() {
        Node.Pos += By;

        Entity.ClearRoomRenderCache();
        Entity.OnChanged();

        return true;
    }

    public void Undo() {
        Node.Pos -= By;

        Entity.ClearRoomRenderCache();
        Entity.OnChanged();
    }
}
