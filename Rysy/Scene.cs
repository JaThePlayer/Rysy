﻿using ImGuiNET;
using Rysy.Gui;
using Rysy.Gui.Windows;

namespace Rysy;

public abstract class Scene {
    private List<Window> Windows = new();

    public HotkeyHandler Hotkeys { get; private set; }

    public Scene() {
        RemoveWindow = (w) => {
            Windows.Remove(w);
        };
    }

    public float TimeActive { get; private set; }

    /// <summary>
    /// Called when this scene is set to <see cref="RysyEngine.Scene"/>
    /// </summary>
    public virtual void OnBegin() {
        SetupHotkeys();
    }

    public virtual void SetupHotkeys() {
        Hotkeys = new(Input.Global, updateInImgui: false);
    }

    public virtual void Update() {
        if (!ImGui.GetIO().WantCaptureKeyboard && !ImGui.GetIO().WantCaptureMouse)
            Hotkeys?.Update();

        TimeActive += Time.Delta;
    }

    public virtual void Render() {

    }

    public virtual void RenderImGui() {
        // Loop in reverse because windows might get removed during the loop.
        for (int i = Windows.Count - 1; i >= 0; i--) {
            Windows[i].RenderGui();
        }
    }

    private Action<Window> RemoveWindow;

    /// <summary>
    /// Adds a window to this scene.
    /// </summary>
    public void AddWindow(Window wind) {
        wind.SetRemoveAction(RemoveWindow);
        Windows.Add(wind);
    }

    /// <summary>
    /// Adds a new window of type <typeparamref name="T"/> if there's no window of that type in the scene.
    /// </summary>
    public void AddWindowIfNeeded<T>() where T : Window, new() {
        if (!Windows.Any(w => w is T))
            AddWindow(new T());
    }

    /// <summary>
    /// Adds a new window of type <typeparamref name="T"/> if there's no window of that type in the scene.
    /// Creates the instance by calling <paramref name="factory"/>
    /// </summary>
    public void AddWindowIfNeeded<T>(Func<T> factory) where T : Window {
        if (!Windows.Any(w => w is T))
            AddWindow(factory());
    }

    public virtual void OnFileDrop(FileDropEventArgs args) {
        // Rysy is most likely not focused, but visible rn. Force the window to be active for a bit, to update the UI.
        RysyEngine.ForceActiveTimer = 1f;
    }

    public bool OnInterval(double interval) {
        if (interval < Time.Delta * 2f)
            interval = Time.Delta * 2f;
        //return true;

        var time = Time.Elapsed;
        return time % interval < Time.Delta;
    }
}
