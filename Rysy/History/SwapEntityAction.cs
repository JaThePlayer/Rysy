namespace Rysy.History;

public record class SwapEntityAction(Entity Orig, Entity With) : IHistoryAction {
    public bool Apply(Map map) {
        if (Orig == With)
            return false;

        var list = Orig.GetRoomList();

        if (list.IndexOf(Orig) is { } idx && idx == -1) {
            return false;
        }

        list[idx] = With;
        With.Id = Orig.Id;

        // make sure that selections now point to the new entity instead of the old one
        Orig.TransferHandlersTo(With);

        return true;
    }

    public void Undo(Map map) {
        var list = With.GetRoomList();

        list[list.IndexOf(With)] = Orig;

        With.TransferHandlersTo(Orig);
    }
}
