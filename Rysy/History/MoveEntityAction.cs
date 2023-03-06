namespace Rysy.History;

public record class MoveEntityAction(Entity Entity, Vector2 By) : IHistoryAction {
    public bool Apply() {
        Entity.Pos += By;

        Entity.ClearRoomRenderCache();

        return true;
    }

    public void Undo() {
        Entity.Pos -= By;

        Entity.ClearRoomRenderCache();
    }
}

public record class MoveDecalAction(Decal Decal, Vector2 By) : IHistoryAction {
    public bool Apply() {
        Decal.Pos += By;

        Decal.ClearRoomRenderCache();

        return true;
    }

    public void Undo() {
        Decal.Pos -= By;

        Decal.ClearRoomRenderCache();
    }
}
