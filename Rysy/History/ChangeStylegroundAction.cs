using Rysy.Stylegrounds;

namespace Rysy.History;

public record class ChangeStylegroundAction(Style Style, Dictionary<string, object> Edited) : IHistoryAction {
    Dictionary<string, object> Old;
    Dictionary<string, object> EditedClone;

    public bool Apply(Map map) {
        Old ??= new(Style.Data.Inner, Style.Data.Inner.Comparer);
        EditedClone ??= new(Edited, Edited.Comparer);

        Style.Data.BulkUpdate(EditedClone);

        return true;
    }

    public void Undo(Map map) {
        Style.Data.BulkUpdate(Old);
    }
}
