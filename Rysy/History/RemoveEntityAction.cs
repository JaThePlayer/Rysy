namespace Rysy.History;

public sealed record class RemoveEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        Entity.ClearInnerCaches();
        var removed = Entity.GetRoomList().Remove(Entity);
        if (removed)
            Entity.Room = null!;
        return removed;
    }

    public void Undo() {
        Entity.Room = Room;
        Entity.GetRoomList().Add(Entity);
    }
}