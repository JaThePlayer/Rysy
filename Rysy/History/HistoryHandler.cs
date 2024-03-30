using Rysy.Extensions;
using Rysy.Scenes;
using System.Text.Json;

namespace Rysy.History;

public class HistoryHandler {
    internal List<IHistoryAction> Actions { get; set; } = new();
    internal List<IHistoryAction> UndoneActions { get; set; } = new();

    internal List<IHistoryAction> SimulatedActions { get; set; } = new();
    
    internal Map Map { get; set; }

    public Action? OnUndo { get; set; }
    public Action? OnApply { get; set; }

    public HistoryHandler(Map map) {
        Map = map;
    }

    public void UndoSimulations() {
        foreach (var item in SimulatedActions.AsEnumerable().Reverse()) {
            item?.Undo(Map);
        }
        SimulatedActions.Clear();
    }

    public void ApplyNewSimulation(IHistoryAction? action) {
        UndoSimulations();

        if (action is { }) {
            action.Apply(Map);
            SimulatedActions.Add(action);
        }
    }

    public void ApplyNewAction(IEnumerable<IHistoryAction?> actions) {
        UndoSimulations();

        if (actions is MergedAction merged) {
            ApplyNewAction((IHistoryAction)merged);
            return;
        }

        List<IHistoryAction> actionList = new();
        foreach (var action in actions) {
            if (action?.Apply(Map) ?? false) {
                actionList.Add(action);
            }
        }

        if (actionList.Count > 0)
            InsertAction(MergedAction.Preapplied(actionList));
    }

    public void ApplyNewAction(MergedAction action)
        => ApplyNewAction((IHistoryAction) action);

    public void ApplyNewAction(IHistoryAction? action) {
        UndoSimulations();

        if (action?.Apply(Map) ?? false) {
            InsertAction(action);
        }
    }

    internal void InsertAction(IHistoryAction action) {
        Actions.Add(action);
        UndoneActions.Clear();
        OnApply?.Invoke();
    }

    public void Undo() {
        UndoSimulations();

        if (Actions.Count > 0) {
            var action = Pop(Actions);
            action.Undo(Map);
            UndoneActions.Add(action);
            OnUndo?.Invoke();
        }
    }

    public void Redo() {
        UndoSimulations();

        if (UndoneActions.Count > 0) {
            var action = Pop(UndoneActions);
            action.Apply(Map);
            Actions.Add(action);
            OnApply?.Invoke();
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

    public string Serialize() {
        var serialized = Actions.Select(act => act is ISerializableAction s ? s.GetSerializable() : null).Where(s => s is not null).ToList();

        var json = serialized.ToJson();

        return json;
    }

    public static List<IHistoryAction> Deserialize(string json) {
        var d = JsonSerializer.Deserialize<List<DeserializedAction>>(json);

        var map = (RysyEngine.Scene as EditorScene)?.Map;
        if (map is null)
            return new();

        List<IHistoryAction> list = new();
        foreach (var item in d!) {
            list.Add(item.ToAction(map));
        }

        return list;
    }

    private struct DeserializedAction {
        public string TypeName { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public ISerializableAction ToAction(Map map) {
            var type = Type.GetType(TypeName);

            var m = type?.GetMethod(nameof(ISerializableAction.FromSerializable));

            var act = (ISerializableAction) m!.Invoke(null, new object[] { map, Data })!;

            return act;
        }
    }
}
