namespace Rysy.History;

public record class ChangeStylegroundAction(Style Style, Dictionary<string, object> Edited) : IHistoryAction {
    Dictionary<string, object> Old;

    public bool Apply() {
        Old = new(Style.Data.Inner);

        foreach (var (key, val) in Edited) {
            if (val is { })
                Style.Data[key] = val;
            else
                Style.Data.Remove(key);
        }

        return true;
    }

    public void Undo() {
        Style.Data.Inner = new(Old);
    }
}
