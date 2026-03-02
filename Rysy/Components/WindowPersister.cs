using Rysy.Gui.Windows;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy.Components;

/// <summary>
/// Persists the given window, even between reloads.
/// </summary>
public sealed class WindowPersister<T>(Func<T> factory, Settings settings, bool defaultState) : SceneComponent, ISignalListener<SettingsChanged<bool>> where T : Window {
    public void OnSignal(SettingsChanged<bool> signal) {
        Toggle(Scene);
    }

    public override void OnAdded() {
        base.OnAdded();
        Toggle(Scene);
    }

    public override void OnRemoved() {
        foreach (var window in Scene?.GetAll<T>() ?? []) {
            window.RemoveSelf();
        }
        base.OnRemoved();
    }

    public void Toggle(Scene? scene) {
        if (scene is null)
            return;
        var enabled = settings.IsWindowPersisted<T>(defaultState);

        if (enabled) {
            if (scene.Get<T>() is null) {
                var w = factory();
                w.SetRemoveAction(_ => settings.TogglePersistedWindow<T>(false));
                scene.AddWindow(w);
            }
        } else {
            foreach (var window in scene.GetAll<T>()) {
                window.RemoveSelf();
            }
        }
    }
}