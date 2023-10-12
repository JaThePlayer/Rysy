﻿using ImGuiNET;

namespace Rysy.Gui.Windows;

public class Window {
    private Action<Window> RemoveSelfImpl;

    public readonly string Name;
    public NumVector2? Size;
    public bool Resizable;

    private bool _ForceResize = false;

    public void ForceSetSize(NumVector2 size) {
        Size = size;
        _ForceResize = true;
    }

    /// <summary>
    /// The ID used for ImGui.Begin, which guarantees that multiple windows with the same name with pop up seperately.
    /// </summary>
    private string WindowID;

    private bool _NoSaveData = true;
    /// <summary>
    /// Tells imgui not to store any data about this window to its ini file.
    /// Use for auto-generated windows.
    /// </summary>
    public bool NoSaveData {
        get => _NoSaveData;
        set {
            if (value != _NoSaveData) {
                _NoSaveData = value;

                GenerateID();
            }
        }
    }

    /// <summary>
    /// Controls whether this window can be moved or not.
    /// </summary>
    public bool NoMove { get; set; } = false;

    private void GenerateID() {
        if (NoSaveData)
            WindowID = $"{Name}##{Guid.NewGuid()}";
        else
            WindowID = Name;
    }

    public Window(string name, NumVector2? size = null) {
        Name = name;
        Size = size;

        GenerateID();
    }

    public void SetRemoveAction(Action<Window> removeSelf) => RemoveSelfImpl += removeSelf;

    public void RenderGui() {
        if (!Visible)
            return;

        if (Size is { } size)
            ImGui.SetNextWindowSize(size, _ForceResize ? ImGuiCond.Always : ImGuiCond.Once);
        

        ImGuiManager.PushWindowStyle();
        var open = true;

        var flags = (Resizable && !_ForceResize) ? ImGuiManager.WindowFlagsResizable : ImGuiManager.WindowFlagsUnresizable;

        if (NoSaveData || _ForceResize)
            flags |= ImGuiWindowFlags.NoSavedSettings;
        _ForceResize = false;

        if (NoMove)
            flags |= ImGuiWindowFlags.NoMove;

        if (ImGui.Begin(WindowID, ref open, flags)) {
            if (HasBottomBar) {
                ImGuiManager.WithBottomBar(Render, RenderBottomBar);
            } else {
                Render();
            }
        }
        ImGui.End();

        if (!open) {
            RemoveSelf();
        }

        ImGuiManager.PopWindowStyle();
    }

    protected virtual void Render() {

    }

    protected virtual bool HasBottomBar => false;

    protected virtual void RenderBottomBar() {

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
