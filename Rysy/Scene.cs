using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Scenes;
using Rysy.Signals;
using Rysy.Signals.Hotkeys;

namespace Rysy;

public abstract class Scene {
    private readonly List<(string Id, Action Render)> _popups = [];
    private readonly Queue<string> _newPopupQueue = [];

    private ComponentRegistryScope? _components;

    public IComponentRegistry Components => _components ?? throw new Exception("Components are not initialised yet. They may not be added until OnBegin.");

    public HotkeyHandler Hotkeys { get; private set; }
    public HotkeyHandler HotkeysIgnoreImGui { get; private set; }

    public IRysyLoggerFactory LoggerFactory => GetRequired<IRysyLoggerFactory>();
    public IRysyLogger Logger => this.AddIfMissing(self => self.LoggerFactory.CreateLogger(self.GetType()));

    protected Scene() {
        //_components = new SceneComponentRegistry();
        _removeWindow = (w) => {
            w.Removed();
            Remove(w);
        };
    }

    public float TimeActive { get; private set; }

    internal void SetGlobalComponentRegistry(IComponentRegistry globalComponents) {
       _components = new ComponentRegistryScope(globalComponents);
    }
    
    /// <summary>
    /// Called when this scene is set to <see cref="RysyEngine.Scene"/>
    /// </summary>
    public virtual void OnBegin() {
        SetupHotkeys();
        
        foreach (var c in GetAll<SceneComponent>()) {
            c.OnAdded();
        }
    }

    /// <summary>
    /// Called when this scene is unset from <see cref="RysyEngine.Scene"/>
    /// </summary>
    public virtual void OnEnd() {
        foreach (var c in GetAll<SceneComponent>()) {
            c.OnRemoved();
        }
        
        _components?.Dispose();
        _components = null;
    }

    public virtual void SetupHotkeys() {
        Hotkeys = new(Input.Global, HotkeyHandler.ImGuiModes.Never);
        HotkeysIgnoreImGui = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
        Add(Hotkeys);
        Add(HotkeysIgnoreImGui);
        
        HotkeysIgnoreImGui.AddSignalHotkeyFromSettings<HotkeyCloseWindow>();
        HotkeysIgnoreImGui.AddSignalHotkeyFromSettings<HotkeyCloseWindowAndSave>();
    }

    public virtual void Update() {
        Hotkeys?.Update();
        HotkeysIgnoreImGui?.Update();

        TimeActive += Time.Delta;
        
        foreach (var c in EnumerateAllLocked<SceneComponent>()) {
            c.Update();
        }
    }

    public virtual void Render() {
        foreach (var c in EnumerateAllLocked<SceneComponent>()) {
            c.Render();
        }
    }

    public virtual void RenderImGui() {
        if (_components is { } registry) {
            foreach (var c in registry.EnumerateAllLocked<SceneComponent>()) {
                c.RenderImGui();
            }
        
            foreach (var t in registry.EnumerateAllLocked<Window>()) {
                t.RenderGui();
            }
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

    internal void ConfigureNewWindow(Window window) {
        var managers = GetAll<INewWindowManager>();
        WindowStartConfig? maybeConfig = null;
        foreach (var manager in managers) {
            if (manager.Layout(this, window, RysyState.GraphicsDevice.Viewport.Bounds) is { } cfg) {
                maybeConfig = cfg;
                break;
            }
        }

        if (maybeConfig is not {} config)
            return;
        
        window.OnFirstRender += () => ImGui.SetNextWindowPos(config.Position, ImGuiCond.Always, pivot: new(0f));
    }
    
    /// <summary>
    /// Adds a window to this scene.
    /// </summary>
    public void AddWindow(Window wind) {
        wind.SetRemoveAction(_removeWindow);
        wind.Added(this);
        ConfigureNewWindow(wind);
        Add(wind);
    }

    /// <summary>
    /// Adds a new window of type <typeparamref name="T"/> if there's no window of that type in the scene.
    /// </summary>
    public T AddWindowIfNeeded<T>() where T : Window, new() {
        var window = GetAll<Window>().OfType<T>().FirstOrDefault();

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
        var existing = GetAll<Window>().FirstOrDefault(w => w is T);

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
        if (!GetAll<Window>().Any(w => w is T))
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

    public void Add<T>(T component) {
        if (component is not null)
            Components.Add(component);
    }
    
    public void Remove<T>(T component) {
        if (component is not null)
            Components.Remove(component);
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
    
    public IReadOnlyList<T> GetAll<T>() where T : class {
        return _components is null ? [] : Components.GetAll<T>();
    }
    
    public IReadOnlyList<T> GetAll<T>(Type targetType) where T : class {
        return _components is null ? [] : Components.GetAll<T>(targetType);
    }
    
    public ComponentRegistryExt.EnumerateAllLockedEnumerable<T> EnumerateAllLocked<T>() where T : class {
        if (_components is null)
            return default;
        return Components.EnumerateAllLocked<T>();
    }
    
    public void Emit<T>(T signal) where T : ISignal {
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
