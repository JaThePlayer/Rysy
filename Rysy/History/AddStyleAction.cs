namespace Rysy.History;

public record class AddStyleAction(IList<Style> Styles, Style NewStyle, int? Index, StyleFolder? Parent) : IHistoryAction {
    public bool Apply() {
        NewStyle.Parent = Parent;

        if (Index is { } index) {
            Styles.Insert(index, NewStyle); 
            return true;
        }

        Styles.Add(NewStyle);
        return true;
    }

    public void Undo() {
        Styles.Remove(NewStyle);
        NewStyle.Parent = null;
    }
}
