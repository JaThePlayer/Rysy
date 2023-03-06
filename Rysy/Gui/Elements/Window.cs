using ImGuiNET;

namespace Rysy.Gui;

public record class Window {
    private Action<Window> RemoveSelfImpl;

    public readonly string Name;
    public Action<Window>? Render;
    public NumVector2? Size;

    /// <summary>
    /// The ID used for ImGui.Begin, which guarantees that multiple windows with the same name with pop up seperately.
    /// </summary>
    private string WindowID;

    public Window(string name, Action<Window>? render, NumVector2? size = null) {
        Name = name;
        Render = render;
        Size = size;
        WindowID = $"{Name}##{Guid.NewGuid()}";
    }

    public Window(string name, NumVector2? size = null) : this(name, null, size) {

    }

    public void SetRemoveAction(Action<Window> removeSelf) => RemoveSelfImpl += removeSelf;

    public void RenderGui() {
        if (Render is null)
            return;

        if (Size is { } size)
            ImGui.SetNextWindowSize(size);

        ImGuiManager.PushWindowStyle();
        var open = true;

        if (ImGui.Begin(WindowID, ref open, Size is { } ? ImGuiManager.WindowFlagsUnresizable : ImGuiManager.WindowFlagsResizable)) {
            Render(this);
        }
        ImGui.End();

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    public void RemoveSelf() {
        RemoveSelfImpl?.Invoke(this);
    }
}

/// <summary>
/// A window, which stores arbitrary data of type <typeparamref name="T"/>.
/// </summary>
public record class Window<T> : Window {
    /// <summary>
    /// Extra data attached to this window.
    /// </summary>
    public T Data;

    public Window(string Name, Action<Window<T>> Render, NumVector2? Size = null) : base(Name, (w) => Render((Window<T>)w), Size) {

    }

    public Window(string Name, T data, Action<Window<T>> Render, NumVector2? Size = null) : base(Name, (w) => Render((Window<T>) w), Size) {
        Data = data;
    }
}
