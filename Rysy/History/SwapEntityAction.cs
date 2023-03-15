namespace Rysy.History;

public record class SwapEntityAction(Entity Orig, Entity With) : IHistoryAction {
    public bool Apply() {
        if (Orig == With)
            return false;

        var list = Orig.GetRoomList();

        if (list.IndexOf(Orig) is { } idx && idx == -1)
            return false;

        list[idx] = With;

        return true;
    }

    public void Undo() {
        var list = With.GetRoomList();

        list[list.IndexOf(With)] = Orig;
    }
}
