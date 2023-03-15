namespace Rysy.History;

public sealed record class RemoveEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        return Entity.GetRoomList().Remove(Entity);
    }

    public void Undo() {
        Entity.GetRoomList().Add(Entity);
    }
}