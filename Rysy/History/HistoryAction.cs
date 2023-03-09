namespace Rysy.History;

public interface IHistoryAction {
    /// <returns>Whether the action had any effect. If false is returned, the action will not be added to history</returns>
    public bool Apply();
    public void Undo();
}

public static class HistoryActionExtensions {
    public static MergedAction MergeActions(this IEnumerable<IHistoryAction?> actions) => new(actions);

    public static HookedAction WithHook(this IHistoryAction action, Action? onApply = null, Action? onUndo = null) => new(action) {
        OnApply = onApply,
        OnUndo = onUndo,
    };
}

