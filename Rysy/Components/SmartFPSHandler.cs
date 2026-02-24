using Rysy.Gui;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy.Components;

public sealed class SmartFpsHandler : SceneComponent, ISignalListener<SettingsChanged<bool>> {
    private float _inactiveTimer;
    private bool _loweredFps;

    private bool _enabled;

    public void OnToggle(Settings settings, bool toggle) {
        _enabled = toggle;
        _inactiveTimer = 0f;

        if (!toggle) {
            // Make sure to reset the target FPS or it could get stuck on low fps forever...
            RysyEngine.SetTargetFps(settings?.TargetFps ?? 60);
        }
    }

    public override void Update() {
        if (!_enabled)
            return;

        var input = Input.Global;

        if (input.Mouse.PositionDelta == Point.Zero 
        && input.Mouse.ScrollDelta == 0 
        && !input.Keyboard.AnyKeyHeld 
        && !input.Mouse.AnyClickedOrHeld
        && !ImGuiManager.WantCaptureMouse) {
            if (_inactiveTimer > 2f && !_loweredFps) {

                RysyEngine.SetTargetFps(10);
                _loweredFps = true;
            }
            else
                _inactiveTimer += Time.Delta;
        } else {
            if (_inactiveTimer > 0f && _loweredFps)
                RysyEngine.SetTargetFps(Settings.Instance.TargetFps);
            _inactiveTimer = 0f;
            _loweredFps = false;
        }
    }

    public void OnSignal(SettingsChanged<bool> signal) {
        if (signal.SettingName == nameof(Settings.SmartFramerate)) {
            OnToggle(signal.Settings, signal.Value);
        }
    }
}
