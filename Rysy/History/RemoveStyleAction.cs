using Rysy.Stylegrounds;

namespace Rysy.History;
public record class RemoveStyleAction(IList<Style> Styles, Style ToRemove, StyleFolder? Folder) : IHistoryAction {
    private int _index;
    private bool _removedFromParent;

    public bool Apply(Map map) {
        _index = Styles.IndexOf(ToRemove);
        if (ToRemove.Parent == Folder) {
            ToRemove.Parent = null;
            _removedFromParent = true;
        }

        return Styles.Remove(ToRemove);
    }

    public void Undo(Map map) {
        Styles.Insert(_index, ToRemove);
        if (_removedFromParent) {
            ToRemove.Parent = Folder;
        }
    }
}
