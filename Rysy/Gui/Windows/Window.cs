using Hexa.NET.ImGui;
using Rysy.Helpers;
using Rysy.Signals;

namespace Rysy.Gui.Windows;

public class Window : ISignalEmitter {
    public Scene Scene { get; private set; }

    public Theme Theme => Scene.GetRequired<Themes>().Current;

    public IRysyLogger Logger => field ??= Scene.LoggerFactory.CreateLogger(GetType());
    
    private Action<Window> _removeSelfImpl;

    public readonly string Name;
    public NumVector2? Size;
    public bool Resizable;

    private bool _forceResize = false;

    private bool _firstRender = true;

    public void ForceSetSize(NumVector2 size) {
        Size = size;
        _forceResize = true;
    }

    /// <summary>
    /// The ID used for ImGui.Begin, which guarantees that multiple windows with the same name with pop up seperately.
    /// </summary>
    public string WindowId;

    /// <summary>
    /// Tells imgui not to store any data about this window to its ini file.
    /// Use for auto-generated windows.
    /// </summary>
    public bool NoSaveData {
        get;
        set {
            if (value != field) {
                field = value;

                GenerateId();
            }
        }
    } = true;

    /// <summary>
    /// Controls whether this window can be moved or not.
    /// </summary>
    public bool NoMove { get; set; } = false;

    public bool Closeable { get; set; } = true;

    public virtual bool PersistBetweenScenes => false;
    
    public NumVector2 LastPosition { get; private set; }
    
    public NumVector2 LastSize { get; private set; }
    
    public uint LastDockId { get; private set; }
    
    public bool LastWasDocked { get; private set; }
    
    public Rectangle LastBounds => new Rectangle((int)LastPosition.X, (int)LastPosition.Y, (int)LastSize.X, (int)LastSize.Y);

    internal Action? OnFirstRender { get; set; }

    private void GenerateId() {
        if (NoSaveData)
            WindowId = $"{Name}##{Guid.NewGuid()}";
        else
            WindowId = Name;
    }

    public Window(string name, NumVector2? size = null) {
        Name = name;
        Size = size;

        GenerateId();
    }

    public void SetRemoveAction(Action<Window> removeSelf) => _removeSelfImpl = removeSelf;

    public void RenderGui() {
        if (!Visible)
            return;

        if (Size is { } size)
            ImGui.SetNextWindowSize(size, _forceResize ? ImGuiCond.Always : ImGuiCond.Once);
        if (_firstRender) {
            _firstRender = false;
            OnFirstRender?.Invoke();
        }

        ImGuiManager.PushWindowStyle();
        var open = true;

        var flags = (Resizable && !_forceResize) ? ImGuiManager.WindowFlagsResizable : ImGuiManager.WindowFlagsUnresizable;

        if (NoSaveData || _forceResize)
            flags |= ImGuiWindowFlags.NoSavedSettings;
        _forceResize = false;

        if (NoMove)
            flags |= ImGuiWindowFlags.NoMove;

        var isOpen = Closeable
            ? ImGui.Begin(WindowId, ref open, EditWindowFlags(flags))
            : ImGui.Begin(WindowId, EditWindowFlags(flags));
        
        try {
            if (isOpen) {
                if (HasBottomBar) {
                    ImGuiManager.WithBottomBar(Render, RenderBottomBar, (uint)string.GetHashCode(Interpolator.Temp($"{WindowId}.child"), StringComparison.Ordinal));
                } else {
                    Render();
                }
            }
        } finally {
            LastPosition = ImGui.GetWindowPos();
            LastSize = ImGui.GetWindowSize();
            LastDockId = ImGui.GetWindowDockID();
            LastWasDocked = ImGui.IsWindowDocked();
            ImGui.GetWindowViewport();
            ImGui.End();
        }

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    protected internal virtual void Added(Scene scene) {
        Scene = scene;
    }

    protected internal virtual void Removed() {
        Scene = null!;
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

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
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
