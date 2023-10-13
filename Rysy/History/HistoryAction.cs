namespace Rysy.History;

public interface IHistoryAction {
    /// <returns>Whether the action had any effect. If false is returned, the action will not be added to history</returns>
    public bool Apply();
    public void Undo();

    public static IHistoryAction Empty => new MergedAction(Array.Empty<IHistoryAction>());
}

public interface ISerializableAction : IHistoryAction {
    public Dictionary<string, object> GetSerializableData();
    public static abstract ISerializableAction FromSerializable(Map map, Dictionary<string, object> data);
}

public static class SerializableActionExt {
    public static ActionData? GetSerializable(this ISerializableAction action) {
        ArgumentNullException.ThrowIfNull(action);

        var data = action.GetSerializableData();
        if (data is { })
            return new ActionData() { 
                Data = data,
                TypeName = action.GetType().FullName!,
            };

        return null;
    }
}

public class ActionData {
    public string TypeName { get; set; }

    public object Data { get; set; }
}

public static class HistoryActionExtensions {
    public static MergedAction MergeActions(this IEnumerable<IHistoryAction?> actions) => new(actions);

    public static HookedAction WithHook(this IHistoryAction action, Action? onApply = null, Action? onUndo = null) => new(action) {
        OnApply = onApply,
        OnUndo = onUndo,
    };
}

