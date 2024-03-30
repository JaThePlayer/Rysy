namespace Rysy.History;

public sealed record RemoveEntityAction(EntityRef Entity, RoomRef Room) : IHistoryAction {
    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        
        entity.ClearInnerCaches();
        var removed = entity.GetRoomList().Remove(entity);
        if (removed)
            entity.Room = null!;
        return removed;
    }

    public void Undo(Map map) {
        var entity = Entity.Resolve(map);
        var room = Room.Resolve(map);
        
        entity.Room = room;
        entity.GetRoomList().Add(entity);
    }
}