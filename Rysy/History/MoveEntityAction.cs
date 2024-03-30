namespace Rysy.History;

public record MoveEntityAction(EntityRef Entity, Vector2 By) : IHistoryAction {
    public bool Apply(Map map) {
        var e = Entity.Resolve(map);
        
        e.Pos += By;

        e.ClearRoomRenderCache();

        return true;
    }

    public void Undo(Map map) {
        var e = Entity.Resolve(map);
        
        e.Pos -= By;

        e.ClearRoomRenderCache();
    }
}
