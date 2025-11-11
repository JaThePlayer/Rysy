using Rysy.Stylegrounds;

namespace Rysy.History;

public record class ReorderStyleAction(IList<Style> Styles, Style ToMove, int Offset) : IHistoryAction {
    private int _startIdx;

    public bool Apply(Map map) {
        _startIdx = Styles.IndexOf(ToMove);
        if (_startIdx == -1) {
            return false;
        }

        var i = _startIdx + Offset;
        if (i < 0 || i >= Styles.Count) {
            return false;
        }

        (Styles[i], Styles[_startIdx]) = (Styles[_startIdx], Styles[i]);

        return true;
    }

    public void Undo(Map map) {
        var i = _startIdx + Offset;

        (Styles[i], Styles[_startIdx]) = (Styles[_startIdx], Styles[i]);
    }
}
