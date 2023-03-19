namespace Rysy.History;

public sealed record class AddEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        if (Entity is not Decal && (Entity.ID < 1 || Room.TryGetEntityById(Entity.ID) is not null)) {
            Entity.ID = Room.NextEntityID();
        }
        Entity.GetRoomList().Add(Entity);

        return true;
    }

    public void Undo() {
        Entity.GetRoomList().Remove(Entity);
    }

    public static IHistoryAction AddAll(IEnumerable<Entity> entities, Room room) {
        return new MergedAction(entities.Select(e => new AddEntityAction(e, room)));
    }
}
