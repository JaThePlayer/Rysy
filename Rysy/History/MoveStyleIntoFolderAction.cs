using Rysy.Stylegrounds;

namespace Rysy.History;

public record class MoveStyleIntoFolderAction(Style ToMove, StyleFolder Into, IList<Style> OldStyles, bool FromTop) : IHistoryAction {
    private AddStyleAction _addStyleAction;
    private RemoveStyleAction _removeStyleAction;

    public bool Apply(Map map) {
        _addStyleAction = new AddStyleAction(Into.Styles, ToMove, FromTop ? 0 : Into.Styles.Count, Into);
        _removeStyleAction = new RemoveStyleAction(OldStyles, ToMove, null);

        _removeStyleAction.Apply(map);
        _addStyleAction.Apply(map);
        return true;
    }

    public void Undo(Map map) {
        _addStyleAction.Undo(map);
        _removeStyleAction.Undo(map);
    }
}

public record class MoveStyleOutOfFolderAction(Style ToMove, IList<Style> Into, StyleFolder? IntoFolder, StyleFolder From, bool FromTop) : IHistoryAction {
    private AddStyleAction _addStyleAction;
    private RemoveStyleAction _removeStyleAction;

    public bool Apply(Map map) {
        _addStyleAction = new AddStyleAction(Into, ToMove, Into.IndexOf(From) + (FromTop ? 0 : 1), IntoFolder);
        _removeStyleAction = new RemoveStyleAction(From.Styles, ToMove, From);

        _removeStyleAction.Apply(map);
        _addStyleAction.Apply(map);
        return true;
    }

    public void Undo(Map map) {
        _addStyleAction.Undo(map);
        _removeStyleAction.Undo(map);
    }
}