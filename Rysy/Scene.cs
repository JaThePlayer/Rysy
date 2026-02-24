using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy;

public abstract class Scene : ISignalListener {
    private readonly List<Window> _windows = [];
    
    internal IReadOnlyList<Window> ActiveWindows => _windows;
    
    private readonly List<(string Id, Action Render)> _popups = [];
    private readonly Queue<string> _newPopupQueue = [];

    private SceneComponentRegistry Components { get; }

    public HotkeyHandler Hotkeys { get; private set; }
    public HotkeyHandler HotkeysIgnoreImGui { get; private set; }

    public IRysyLoggerFactory LoggerFactory => GetRequired<IRysyLoggerFactory>();
    public IRysyLogger Logger => this.AddIfMissing(self => self.LoggerFactory.CreateLogger(self.GetType()));

    protected Scene() {
        Components = new SceneComponentRegistry();
        _removeWindow = (w) => {
            RysyState.OnEndOfThisFrame += () => {
                w.Removed();
                _windows.Remove(w);
            };
        };
    }

    public float TimeActive { get; private set; }

    /// <summary>
    /// Called when this scene is set to <see cref="RysyEngine.Scene"/>
    /// </summary>
    public virtual void OnBegin(IComponentRegistry globalComponents) {
        Components.GlobalRegistry = globalComponents;
        SetupHotkeys();
        
        foreach (var c in GetAll<SceneComponent>()) {
            c.Scene = this;
            c.OnBegin();
        }
    }

    /// <summary>
    /// Called when this scene is unset from <see cref="RysyEngine.Scene"/>
    /// </summary>
    public virtual void OnEnd() {
        foreach (var c in GetAll<SceneComponent>()) {
            c.OnEnd();
            c.Scene = null!;
        }
    }

    public virtual void SetupHotkeys() {
        Hotkeys = new(Input.Global, HotkeyHandler.ImGuiModes.Never);
        HotkeysIgnoreImGui = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
    }

    public virtual void Update() {
        Hotkeys?.Update();
        HotkeysIgnoreImGui?.Update();

        TimeActive += Time.Delta;
        
        foreach (var c in GetAll<SceneComponent>()) {
            c.Update();
        }
    }

    public virtual void Render() {
        foreach (var c in GetAll<SceneComponent>()) {
            c.Render();
        }
    }

    public virtual void RenderImGui() {
        foreach (var c in GetAll<SceneComponent>()) {
            c.RenderImGui();
        }
        
        for (int i = 0; i < _windows.Count; i++) {
            _windows[i].RenderGui();
        }

        while (_newPopupQueue.TryDequeue(out var id)) {
            ImGui.OpenPopup(id);
        }
        
        for (int i = _popups.Count - 1; i >= 0; i--) {
            var popup = _popups[i];

            if (ImGui.BeginPopup(popup.Id)) {
                try {
                    popup.Render();
                } finally {
                    ImGui.EndPopup();
                }
            } else {
                _popups.RemoveAt(i);
            }
        }
    }

    private readonly Action<Window> _removeWindow;

    /// <summary>
    /// Adds a window to this scene.
    /// </summary>
    public void AddWindow(Window wind) {
        wind.SetRemoveAction(_removeWindow);
        wind.Added(this);
        _windows.Add(wind);
    }

    /// <summary>
    /// Adds a new window of type <typeparamref name="T"/> if there's no window of that type in the scene.
    /// </summary>
    public T AddWindowIfNeeded<T>() where T : Window, new() {
        var window = _windows.OfType<T>().FirstOrDefault();

        if (window is { })
            return window;
        window = new T();
        AddWindow(window);
        return window;
    }

    /// <summary>
    /// Toggles the window of type <typeparamref name="T"/>
    /// </summary>
    public void ToggleWindow<T>() where T : Window, new() {
        var existing = _windows.FirstOrDefault(w => w is T);

        if (existing is { })
            _removeWindow(existing);
        else
            AddWindow(new T());
    }

    /// <summary>
    /// Adds a new window of type <typeparamref name="T"/> if there's no window of that type in the scene.
    /// Creates the instance by calling <paramref name="factory"/>
    /// </summary>
    public void AddWindowIfNeeded<T>(Func<T> factory) where T : Window {
        if (!_windows.Any(w => w is T))
            AddWindow(factory());
    }

    public void AddPopup(string id, Action renderImgui) {
        _popups.Add((id, renderImgui));
        _newPopupQueue.Enqueue(id);
    }

    protected internal virtual void OnFileDrop(string filePath) {
        // Rysy is most likely not focused, but visible rn. Force the window to be active for a bit, to update the UI.
        RysyState.ForceActiveTimer = 1f;
    }

    public bool OnInterval(double interval) {
        if (interval < Time.Delta * 2f)
            interval = Time.Delta * 2f;
        //return true;

        var time = Time.Elapsed;
        return time % interval < Time.Delta;
    }

    public void Add(object sceneComponent) {
        Components.Add(sceneComponent);
    }
    
    public void Remove(object sceneComponent) {
        Components.Remove(sceneComponent);
    }
    
    public T AddIfMissing<T>() where T : class, new() {
        return Components.AddIfMissing<T>();
    }
    
    public T AddIfMissing<T>(T newValue) where T : class {
        return Components.AddIfMissing(newValue);
    }
    
    public T? Get<T>() where T : class {
        return Components.Get<T>();
    }
    
    public T GetRequired<T>() where T : class {
        return Components.GetRequired<T>();
    }
    
    public IEnumerable<T> GetAll<T>() where T : class {
        return Components.GetAll<T>();
    }

    public void OnSignal<T>(T signal) where T : ISignal {
        Components.OnSignal(signal);
    }
}

public static class SceneExt {
    extension<T>(T scene) where T : Scene {
        public TComponent AddIfMissing<TComponent>(Func<T, TComponent> newValue) where TComponent : class {
            if (scene.Get<TComponent>() is not { } ret) {
                scene.Add(ret = newValue.Invoke(scene));
            }
        
            return ret;
        }
    }
}

internal sealed class SceneComponentRegistry : IComponentRegistry {
    private readonly ComponentRegistry _sceneSpecific = new();
    
    public IComponentRegistry? GlobalRegistry { get; set; }
    
    public void Add(object component) {
        _sceneSpecific.Add(component);
    }

    public void Remove(object component) {
        _sceneSpecific.Remove(component);
    }

    public T? Get<T>() where T : class {
        return _sceneSpecific.Get<T>() ?? GlobalRegistry?.Get<T>();
    }

    public IEnumerable<T> GetAll<T>() where T : class {
        if (GlobalRegistry is { } globalRegistry) {
            return globalRegistry.GetAll<T>().Concat(_sceneSpecific.GetAll<T>());
        }
        return _sceneSpecific.GetAll<T>();
    }
}
