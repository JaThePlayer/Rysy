namespace Rysy.History;

/// <summary>
/// Allows adding a callback whenever Apply or Undo are called, which will NOT get serialized.
/// </summary>
public record class HookedAction(IHistoryAction Parent) : IHistoryAction {
    public Action? OnApply { get; set; }
    public Action? OnUndo { get; set; }


    public bool Apply() {
        var ret = Parent.Apply();
        OnApply?.Invoke();

        return ret;
    }

    public void Undo() {
        Parent.Undo();
        OnUndo?.Invoke();
    }

    public override string ToString() {
        return Parent.ToString()!;
    }
}
