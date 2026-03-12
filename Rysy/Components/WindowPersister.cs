using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy.Components;

internal interface IWindowPersister {
    public void RenderImGuiToggle(Scene scene);
}

/// <summary>
/// Persists the given window, even between reloads.
/// </summary>
public sealed class WindowPersister<T>(Func<T> factory, string langKey, Settings settings, bool defaultState) : SceneComponent, IWindowPersister,
    ISignalListener<SettingsChanged<bool>> where T : Window {
    
    public void OnSignal(SettingsChanged<bool> signal) {
        Toggle(Scene);
    }

    public override void OnAdded() {
        base.OnAdded();
        Toggle(Scene);
    }

    public override void OnRemoved() {
        if (Scene is not null) {
            foreach (var window in Scene.EnumerateAllLocked<T>()) {
                window.RemoveSelf();
            }
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
            foreach (var window in scene.EnumerateAllLocked<T>()) {
                window.RemoveSelf();
            }
        }
    }

    public void RenderImGuiToggle(Scene scene) {
        var enabled = settings.IsWindowPersisted<T>(defaultState);
        if (ImGuiManager.TranslatedCheckbox(langKey, ref enabled)) {
            settings.TogglePersistedWindow<T>(enabled);
            Toggle(scene);
        }
    }
}