using ImGuiNET;

namespace Rysy.Gui;

public class Window {
    private Action<Window> RemoveSelfImpl;

    public readonly string Name;
    public NumVector2? Size;
    public bool Resizable;

    /// <summary>
    /// The ID used for ImGui.Begin, which guarantees that multiple windows with the same name with pop up seperately.
    /// </summary>
    private string WindowID;

    public Window(string name, NumVector2? size = null) {
        Name = name;
        Size = size;
        WindowID = $"{Name}##{Guid.NewGuid()}";
    }

    public void SetRemoveAction(Action<Window> removeSelf) => RemoveSelfImpl += removeSelf;

    public void RenderGui() {
        if (!Visible)
            return;

        if (Size is { } size)
            ImGui.SetNextWindowSize(size);

        ImGuiManager.PushWindowStyle();
        var open = true;

        if (ImGui.Begin(WindowID, ref open, Resizable ? ImGuiManager.WindowFlagsResizable : ImGuiManager.WindowFlagsUnresizable)) {
            Render();
        }
        ImGui.End();

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    protected virtual void Render() {

    }

    protected virtual bool Visible => true;

    public virtual void RemoveSelf() {
        RemoveSelfImpl?.Invoke(this);
    }
}

public class ScriptedWindow : Window {
    public Action<Window> RenderFunc;

    public ScriptedWindow(string name, Action<Window> renderFunc, NumVector2? size = null) : base(name, size) {
        RenderFunc = renderFunc;
    }

    protected override void Render() {
        base.Render();
        RenderFunc(this);
    }
}
