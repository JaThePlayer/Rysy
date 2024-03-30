namespace Rysy.History;

public sealed record class AddEntityAction(Entity Entity, RoomRef Room) : IHistoryAction {
    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        Entity.Room = room;
        
        if (Entity is not Decal && (Entity.Id < 0 || room.TryGetEntityById(Entity.Id) is not null)) {
            Entity.Id = room.NextEntityID();
        }
        Entity.GetRoomList().Add(Entity);

        return true;
    }

    public void Undo(Map map) {
        Entity.GetRoomList().Remove(Entity);
    }

    public static IHistoryAction AddAll(IEnumerable<Entity> entities, Room room) {
        return new MergedAction(entities.Select(e => new AddEntityAction(e, room)));
    }
}
