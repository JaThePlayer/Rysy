namespace Rysy.History;

public class MergedAction : IHistoryAction {
    List<IHistoryAction> Actions;

    public MergedAction(IEnumerable<IHistoryAction> actions) {
        Actions = new(actions);
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
}
