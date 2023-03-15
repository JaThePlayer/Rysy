namespace Rysy.History;

public sealed record class AddEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        Entity.GetRoomList().Add(Entity);

        return true;
    }

    public void Undo() {
        Entity.GetRoomList().Remove(Entity);
    }
}
