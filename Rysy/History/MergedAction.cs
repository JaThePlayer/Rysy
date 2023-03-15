using Rysy.Scenes;
using System.Collections;

namespace Rysy.History;

public record class MergedAction : IHistoryAction, IEnumerable<IHistoryAction>, ISerializableAction {
    List<IHistoryAction> Actions;

    public MergedAction(IEnumerable<IHistoryAction?> actions) {
        Actions = new(actions.Where(act => act is not null)!);
    }

    private bool[] Applied;

    public bool Apply() {
        Applied = new bool[Actions.Count];
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
        for (int i = 0; i < Actions.Count; i++) {
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
