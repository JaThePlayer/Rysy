namespace Rysy.History;

public record class SwapEntityAction(Entity Orig, Entity With) : IHistoryAction {
    public bool Apply() {
        if (Orig == With)
            return false;

        var list = GetList(Orig);

        if (list.IndexOf(Orig) is { } idx && idx == -1)
            return false;

        list[idx] = With;

        return true;
    }

    public void Undo() {
        var list = GetList(With);

        list[list.IndexOf(With)] = Orig;
    }

    private TypeTrackedList<Entity> GetList(Entity entity) {
        return entity is Trigger ? entity.Room.Triggers : entity.Room.Entities;
    }
}
