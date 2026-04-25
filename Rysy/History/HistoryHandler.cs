using Rysy.Signals;
using System.Text.Json;

namespace Rysy.History;

public sealed class HistoryHandler : ISignalEmitter, IHistoryHandler {
    internal List<IHistoryAction> Actions { get; set; } = new();
    internal List<IHistoryAction> UndoneActions { get; set; } = new();

    internal List<IHistoryAction> SimulatedActions { get; set; } = new();
    
    public Map Map { get; set; }

    public event Action? OnUndo;

    public event Action? OnApply;

    public HistoryHandler(Map map) {
        Map = map;
    }

    public void UndoSimulations() {
        foreach (var item in SimulatedActions.AsEnumerable().Reverse()) {
            if (item is { }) {
                item.Undo(Map);
                this.Emit(new HistoryActionSimulationUndone(this, item));
            }
        }
        SimulatedActions.Clear();
    }

    public void ApplyNewSimulation(IHistoryAction? action) {
        UndoSimulations();

        if (action is { }) {
            action.Apply(Map);
            SimulatedActions.Add(action);
            this.Emit(new HistoryActionSimulationApplied(this, action));
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
        this.Emit(new HistoryActionApplied(this, action));
    }

    public void Undo() {
        UndoSimulations();

        if (Actions.Count > 0) {
            var action = Pop(Actions);
            action.Undo(Map);
            UndoneActions.Add(action);
            OnUndo?.Invoke();
            this.Emit(new HistoryActionUndone(this, action));
        }
    }

    public void Redo() {
        UndoSimulations();

        if (UndoneActions.Count > 0) {
            var action = Pop(UndoneActions);
            action.Apply(Map);
            Actions.Add(action);
            OnApply?.Invoke();
            this.Emit(new HistoryActionApplied(this, action));
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

        var map = EditorState.Current?.Map;
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

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}

public interface IHistoryHandler {
    Map Map { get; set; }
    
    event Action? OnUndo;

    event Action? OnApply;

    void UndoSimulations();

    void ApplyNewSimulation(IHistoryAction? action);

    void ApplyNewAction(IEnumerable<IHistoryAction?> actions);
    
    void ApplyNewAction(MergedAction action)
        => ApplyNewAction((IHistoryAction) action);

    void ApplyNewAction(IHistoryAction? action);

    void Undo();

    void Redo();

    void Clear();
}
