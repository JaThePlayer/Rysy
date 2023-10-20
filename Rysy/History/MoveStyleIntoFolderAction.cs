using Rysy.Stylegrounds;

namespace Rysy.History;

public record class MoveStyleIntoFolderAction(Style ToMove, StyleFolder Into, IList<Style> OldStyles, bool FromTop) : IHistoryAction {
    private AddStyleAction AddStyleAction;
    private RemoveStyleAction RemoveStyleAction;

    public bool Apply() {
        AddStyleAction = new AddStyleAction(Into.Styles, ToMove, FromTop ? 0 : Into.Styles.Count, Into);
        RemoveStyleAction = new RemoveStyleAction(OldStyles, ToMove, null);

        RemoveStyleAction.Apply();
        AddStyleAction.Apply();
        return true;
    }

    public void Undo() {
        AddStyleAction.Undo();
        RemoveStyleAction.Undo();
    }
}

public record class MoveStyleOutOfFolderAction(Style ToMove, IList<Style> Into, StyleFolder? IntoFolder, StyleFolder From, bool FromTop) : IHistoryAction {
    private AddStyleAction AddStyleAction;
    private RemoveStyleAction RemoveStyleAction;

    public bool Apply() {
        AddStyleAction = new AddStyleAction(Into, ToMove, Into.IndexOf(From) + (FromTop ? 0 : 1), IntoFolder);
        RemoveStyleAction = new RemoveStyleAction(From.Styles, ToMove, From);

        RemoveStyleAction.Apply();
        AddStyleAction.Apply();
        return true;
    }

    public void Undo() {
        AddStyleAction.Undo();
        RemoveStyleAction.Undo();
    }
}