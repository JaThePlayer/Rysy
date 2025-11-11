using Rysy.Extensions;
using System.Collections;

namespace Rysy.History;

public record class MergedAction : IHistoryAction, IEnumerable<IHistoryAction>, ISerializableAction {
    public List<IHistoryAction> Actions { get; set; }

    public MergedAction(IEnumerable<IHistoryAction?> actions) {
        Actions = new(actions.Where(act => act is not null)!);
    }

    public MergedAction(params IHistoryAction?[] actions) {
        Actions = new(actions.Where(act => act is not null)!);
    }

    private bool[] _applied;

    internal static MergedAction Preapplied(List<IHistoryAction> actions) {
        var act = new MergedAction();
        act.Actions = actions;
        act._applied = new bool[act.Actions.Count];
        Array.Fill(act._applied, true);

        return act;
    }

    public bool Apply(Map map) {
        _applied ??= new bool[Actions.Count];
        
        Array.Clear(_applied);
        var ret = false;

        for (int i = 0; i < Actions.Count; i++) {
            var action = Actions[i];

            var r = action.Apply(map);
            _applied[i] = r;
            ret |= r;
        }

        return ret;
    }

    public void Undo(Map map) {
        for (int i = Actions.Count - 1; i >= 0; i--) {
            if (_applied[i]) {
                Actions[i].Undo(map);
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
