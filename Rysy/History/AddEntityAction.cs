namespace Rysy.History;

public sealed record class AddEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        var list = GetList();

        list.Add(Entity);

        return true;
    }

    private TypeTrackedList<Entity> GetList() {
        return Entity is Trigger ? Room.Triggers : Room.Entities;
    }

    public void Undo() {
        GetList().Remove(Entity);
    }
}
