using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui.Windows;

public class Window {
    private Action<Window> _removeSelfImpl;

    public readonly string Name;
    public NumVector2? Size;
    public bool Resizable;

    private bool _forceResize = false;

    public void ForceSetSize(NumVector2 size) {
        Size = size;
        _forceResize = true;
    }

    /// <summary>
    /// The ID used for ImGui.Begin, which guarantees that multiple windows with the same name with pop up seperately.
    /// </summary>
    private string _windowId;

    private bool _noSaveData = true;
    /// <summary>
    /// Tells imgui not to store any data about this window to its ini file.
    /// Use for auto-generated windows.
    /// </summary>
    public bool NoSaveData {
        get => _noSaveData;
        set {
            if (value != _noSaveData) {
                _noSaveData = value;

                GenerateId();
            }
        }
    }

    /// <summary>
    /// Controls whether this window can be moved or not.
    /// </summary>
    public bool NoMove { get; set; } = false;

    public bool Closeable { get; set; } = true;

    public virtual bool PersistBetweenScenes => false;

    private void GenerateId() {
        if (NoSaveData)
            _windowId = $"{Name}##{Guid.NewGuid()}";
        else
            _windowId = Name;
    }

    public Window(string name, NumVector2? size = null) {
        Name = name;
        Size = size;

        GenerateId();
    }

    public void SetRemoveAction(Action<Window> removeSelf) => _removeSelfImpl += removeSelf;

    public void RenderGui() {
        if (!Visible)
            return;

        if (Size is { } size)
            ImGui.SetNextWindowSize(size, _forceResize ? ImGuiCond.Always : ImGuiCond.Once);

        ImGuiManager.PushWindowStyle();
        var open = true;

        var flags = (Resizable && !_forceResize) ? ImGuiManager.WindowFlagsResizable : ImGuiManager.WindowFlagsUnresizable;

        if (NoSaveData || _forceResize)
            flags |= ImGuiWindowFlags.NoSavedSettings;
        _forceResize = false;

        if (NoMove)
            flags |= ImGuiWindowFlags.NoMove;

        var isOpen = Closeable
            ? ImGui.Begin(_windowId, ref open, EditWindowFlags(flags))
            : ImGui.Begin(_windowId, EditWindowFlags(flags));
        
        try {
            if (isOpen) {
                if (HasBottomBar) {
                    ImGuiManager.WithBottomBar(Render, RenderBottomBar, (uint)string.GetHashCode(Interpolator.Temp($"{_windowId}.child"), StringComparison.Ordinal));
                } else {
                    Render();
                }
            }
        } finally {
            ImGui.End();
        }

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    protected virtual ImGuiWindowFlags EditWindowFlags(ImGuiWindowFlags prev) => prev;

    protected virtual void Render() {

    }

    public virtual bool HasBottomBar => false;

    public virtual void RenderBottomBar() {

    }

    protected virtual bool Visible => true;

    public virtual void RemoveSelf() {
        _removeSelfImpl?.Invoke(this);
    }
}

public class ScriptedWindow : Window {
    public Action<Window> RenderFunc;

    public Action<Window>? BottomBarRenderFunc;

    public ScriptedWindow(string name, Action<Window> renderFunc, NumVector2? size = null, Action<Window>? bottomBarFunc = null) : base(name, size) {
        RenderFunc = renderFunc;
        BottomBarRenderFunc = bottomBarFunc;
    }

    public override bool HasBottomBar => BottomBarRenderFunc is { };

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        BottomBarRenderFunc?.Invoke(this);
    }

    protected override void Render() {
        base.Render();
        RenderFunc(this);
    }
}
