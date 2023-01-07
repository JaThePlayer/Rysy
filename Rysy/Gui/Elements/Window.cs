using ImGuiNET;

namespace Rysy.Gui;

public record class Window(string Name, Action<Window> Render, NumVector2? Size = null) {
    private Action<Window> RemoveSelfImpl;
    private Guid Guid = Guid.NewGuid();

    /// <summary>
    /// Extra data attached to this window. Can be any arbitrary object.
    /// </summary>
    public object Userdata;

    /// <summary>
    /// Casts and returns <see cref="Userdata"/> to <typeparamref name="T"/>
    /// </summary>
    public T Data<T>() => (T) Userdata;

    public void SetRemoveAction(Action<Window> removeSelf) => RemoveSelfImpl += removeSelf;

    public void RenderGui() {
        if (Size is { } size)
            ImGui.SetNextWindowSize(size);

        ImGuiManager.PushWindowStyle();
        var open = true;
        if (ImGui.Begin($"{Name}##{Guid}", ref open, Size is { } ? ImGuiManager.WindowFlagsUnresizable : ImGuiManager.WindowFlagsResizable)) {
            Render(this);
        }

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    public void RemoveSelf() {
        RemoveSelfImpl?.Invoke(this);
    }
}
