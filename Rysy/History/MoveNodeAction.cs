namespace Rysy.History;

public record class MoveNodeAction(Node Node, Entity Entity, Vector2 By) : IHistoryAction {
    public bool Apply() {
        Node.Pos += By;

        Entity.Room?.ClearRenderCache();

        return true;
    }

    public void Undo() {
        Node.Pos -= By;

        Entity.Room?.ClearRenderCache();
    }
}
