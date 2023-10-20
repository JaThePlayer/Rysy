using Rysy.Stylegrounds;

namespace Rysy.History;

public record class ChangeStylegroundAction(Style Style, Dictionary<string, object> Edited) : IHistoryAction {
    Dictionary<string, object> Old;
    Dictionary<string, object> EditedClone;

    public bool Apply() {
        Old ??= new(Style.Data.Inner, Style.Data.Inner.Comparer);
        EditedClone ??= new(Edited, Edited.Comparer);

        foreach (var (key, val) in EditedClone) {
            if (val is { })
                Style.Data[key] = val;
            else
                Style.Data.Remove(key);
        }

        Style.FakePreviewData = null;

        return true;
    }

    public void Undo() {
        Style.Data.BulkUpdate(new(Old, Old.Comparer));
        Style.FakePreviewData = null;
    }
}
