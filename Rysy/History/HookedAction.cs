namespace Rysy.History;

/// <summary>
/// Allows adding a callback whenever Apply or Undo are called, which will NOT get serialized.
/// </summary>
public record class HookedAction(IHistoryAction Parent) : IHistoryAction {
    public Action? OnApply { get; set; }
    public Action? OnUndo { get; set; }


    public bool Apply(Map map) {
        var ret = Parent.Apply(map);
        OnApply?.Invoke();

        return ret;
    }

    public void Undo(Map map) {
        Parent.Undo(map);
        OnUndo?.Invoke();
    }

    public override string ToString() {
        return Parent.ToString()!;
    }
}
