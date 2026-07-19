using Rysy.Stylegrounds;

namespace Rysy.History;

public record AddStyleAction(IList<Style> Styles, Style NewStyle, int? Index, StyleFolder? Parent) : IHistoryAction {
    public bool Apply(Map map) {
        NewStyle.Parent = Parent;

        if (Index is { } index) {
            if (index > Styles.Count || index < 0)
                return false;
            Styles.Insert(index, NewStyle); 
            return true;
        }

        Styles.Add(NewStyle);
        return true;
    }

    public void Undo(Map map) {
        Styles.Remove(NewStyle);
        NewStyle.Parent = null;
    }
}
