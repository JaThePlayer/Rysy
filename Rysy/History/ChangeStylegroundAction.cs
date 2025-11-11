using Rysy.Stylegrounds;

namespace Rysy.History;

public record class ChangeStylegroundAction(Style Style, Dictionary<string, object> Edited) : IHistoryAction {
    Dictionary<string, object> _old;
    Dictionary<string, object> _editedClone;

    public bool Apply(Map map) {
        _old ??= new(Style.Data.Inner, Style.Data.Inner.Comparer);
        _editedClone ??= new(Edited, Edited.Comparer);

        Style.Data.BulkUpdate(_editedClone);

        return true;
    }

    public void Undo(Map map) {
        Style.Data.BulkUpdate(_old);
    }
}
