using ImGuiNET;

namespace Rysy;
public static class SmartFPSHandler {
    private static float InactiveTimer;
    private static bool LoweredFps;

    public static void OnToggle() {
        InactiveTimer = 0f;

        if (!Enabled) {
            // Make sure to reset the target FPS or it could get stuck on low fps forever...
            RysyEngine.SetTargetFps(Settings.Instance?.TargetFps ?? 60);
        }
    }

    public static bool Enabled => Settings.Instance?.SmartFramerate ?? false;

    public static void Update() {
        if (!Enabled)
            return;

        if (Input.Mouse.PositionDelta == Point.Zero 
        && Input.Mouse.ScrollDelta == 0 
        && !Input.Keyboard.AnyKeyHeld 
        && !Input.Mouse.AnyClicked
        && !ImGui.GetIO().WantCaptureMouse) {
            if (InactiveTimer > 2f && !LoweredFps) {

                RysyEngine.SetTargetFps(10);
                LoweredFps = true;
            }
            else
                InactiveTimer += Time.Delta;
        } else {
            if (InactiveTimer > 0f && LoweredFps)
                RysyEngine.SetTargetFps(Settings.Instance.TargetFps);
            InactiveTimer = 0f;
            LoweredFps = false;
        }
    }
}
