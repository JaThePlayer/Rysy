﻿using Rysy.Extensions;
using System.Collections;

namespace Rysy.History;

public record class MergedAction : IHistoryAction, IEnumerable<IHistoryAction>, ISerializableAction {
    private List<IHistoryAction> Actions;

    public MergedAction(IEnumerable<IHistoryAction?> actions) {
        Actions = new(actions.Where(act => act is not null)!);
        Applied = new bool[Actions.Count];
    }

    public MergedAction(params IHistoryAction?[] actions) {
        Actions = new(actions.Where(act => act is not null)!);
        Applied = new bool[Actions.Count];
    }

    private bool[] Applied;

    internal static MergedAction Preapplied(List<IHistoryAction> actions) {
        var act = new MergedAction();
        act.Actions = actions;
        act.Applied = new bool[act.Actions.Count];
        Array.Fill(act.Applied, true);

        return act;
    }

    public bool Apply() {
        Array.Clear(Applied);
        var ret = false;

        for (int i = 0; i < Actions.Count; i++) {
            var action = Actions[i];

            var r = action.Apply();
            Applied[i] = r;
            ret |= r;
        }

        return ret;
    }

    public void Undo() {
        for (int i = Actions.Count - 1; i >= 0; i--) {
            if (Applied[i]) {
                Actions[i].Undo();
            }
        }
    }

    public override string ToString() {
        return $"{{\n{string.Join("\n    ", Actions.Select(a => a.ToString()))}\n}}";
    }

    public IEnumerator<IHistoryAction> GetEnumerator() {
        return ((IEnumerable<IHistoryAction>) Actions).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable) Actions).GetEnumerator();
    }

    public Dictionary<string, object> GetSerializableData() {
        return new() {
            ["inner"] = Actions.Select(act => act is ISerializableAction s ? s.GetSerializable() : null).Where(s => s is not null).ToList()!
        };
    }

    public static ISerializableAction FromSerializable(Map map, Dictionary<string, object> data) {
        return new MergedAction(HistoryHandler.Deserialize(data["inner"].ToJson()));
    }
}
