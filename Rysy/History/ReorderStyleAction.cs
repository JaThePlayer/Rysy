namespace Rysy.History;

public record class ReorderStyleAction(IList<Style> Styles, Style ToMove, int Offset) : IHistoryAction {
    private int StartIdx;

    public bool Apply() {
        StartIdx = Styles.IndexOf(ToMove);
        if (StartIdx == -1) {
            return false;
        }

        var i = StartIdx + Offset;
        if (i < 0 || i >= Styles.Count) {
            return false;
        }

        (Styles[i], Styles[StartIdx]) = (Styles[StartIdx], Styles[i]);

        return true;
    }

    public void Undo() {
        var i = StartIdx + Offset;

        (Styles[i], Styles[StartIdx]) = (Styles[StartIdx], Styles[i]);
    }
}
