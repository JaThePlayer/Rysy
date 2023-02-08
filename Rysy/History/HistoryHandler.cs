namespace Rysy.History;

public class HistoryHandler {
    private List<IHistoryAction> Actions { get; set; } = new();
    private List<IHistoryAction> UndoneActions { get; set; } = new();

    public void ApplyNewAction(IHistoryAction action) {
        if (action.Apply()) {
            Actions.Add(action);
            UndoneActions.Clear();
        }

    }

    public void Undo() {
        if (Actions.Count > 0) {
            var action = Pop(Actions);
            action.Undo();
            UndoneActions.Add(action);
        }
    }

    public void Redo() {
        if (UndoneActions.Count > 0) {
            var action = Pop(UndoneActions);
            action.Apply();
            Actions.Add(action);
        }
    }

    private static T Pop<T>(List<T> from) {
        var last = from.Last();
        from.RemoveAt(from.Count - 1);

        return last;
    }

    public void Clear() {
        Actions.Clear();
        UndoneActions.Clear();
    }
}
