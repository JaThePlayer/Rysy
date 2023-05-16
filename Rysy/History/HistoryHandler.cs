﻿using Rysy.Extensions;
using Rysy.Scenes;
using System.Text.Json;

namespace Rysy.History;

public class HistoryHandler {
    internal List<IHistoryAction> Actions { get; set; } = new();
    internal List<IHistoryAction> UndoneActions { get; set; } = new();

    public Action? OnUndo { get; set; }
    public Action? OnApply { get; set; }

    public void ApplyNewAction(IHistoryAction action) {
        if (action.Apply()) {
            Actions.Add(action);
            UndoneActions.Clear();
            OnApply?.Invoke();
        }

    }

    public void Undo() {
        if (Actions.Count > 0) {
            var action = Pop(Actions);
            action.Undo();
            UndoneActions.Add(action);
            OnUndo?.Invoke();
        }
    }

    public void Redo() {
        if (UndoneActions.Count > 0) {
            var action = Pop(UndoneActions);
            action.Apply();
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

        var map = (RysyEngine.Scene as EditorScene)!.Map;

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

            var act = (ISerializableAction) m.Invoke(null, new object[] { map, Data })!;

            return act;
        }
    }
}
