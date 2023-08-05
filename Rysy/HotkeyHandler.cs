using ImGuiNET;
using Microsoft.Xna.Framework.Input;

namespace Rysy;

public class HotkeyHandler {
    private List<Hotkey> Hotkeys = new();

    public bool UpdateInImgui = false;

    public Input Input { get; private set; }

    public HotkeyHandler(Input input, bool updateInImgui) {
        Input = input;
        UpdateInImgui = updateInImgui;
    }

    /// <summary>
    /// Adds a new hotkey, loading it from settings using <paramref name="name"/>, saving the hotkey to the settings file using <paramref name="defaultKeybind"/> if it doesn't exist.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defaultKeybind"></param>
    /// <param name="onPress"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public HotkeyHandler AddHotkeyFromSettings(string name, string defaultKeybind, Action onPress, HotkeyModes mode = HotkeyModes.OnClick) {
        return AddHotkey(Settings.Instance.GetOrCreateHotkey(name, defaultKeybind), onPress, mode);
    }

    /// <summary>
    /// Adds a new hotkey, parsed from <paramref name="hotkeyString"/>.
    /// </summary>
    /// <returns>this</returns>
    public HotkeyHandler AddHotkey(string hotkeyString, Action onPress, HotkeyModes mode = HotkeyModes.OnClick) {
        if (hotkeyString is null)
            return this;

        foreach (var splitHotkey in hotkeyString.Split('|')) {
            var hotkey = new Hotkey(splitHotkey) {
                OnPress = onPress,
                Mode = mode
            };

            lock (Hotkeys)
                Hotkeys.Add(hotkey);
        }

        return this;
    }

    public void Update() {
        if (!UpdateInImgui && (ImGui.GetIO().WantCaptureKeyboard || ImGui.GetIO().WantCaptureMouse)) {
            return;
        }
        
        var ctrl = Input.Keyboard.Ctrl();
        var shift = Input.Keyboard.Shift();
        var alt = Input.Keyboard.Alt();
        lock (Hotkeys)
            foreach (var hotkey in Hotkeys) {
                if (hotkey.Ctrl == ctrl && hotkey.Shift == shift && hotkey.Alt == alt) {
                    switch (hotkey.Mode) {
                        case HotkeyModes.OnClick:
                            HandleClickHotkey(hotkey);
                            break;
                        case HotkeyModes.OnHold:
                            HandleHoldHotkey(hotkey);
                            break;
                        case HotkeyModes.OnHoldSmoothInterval:
                            HandleSmoothHotkey(hotkey);
                            break;
                    }
                }
            }
    }

    private void HandleClickHotkey(Hotkey hotkey) {
        if (hotkey.Key is { } key && Input.Keyboard.IsKeyClicked(key)) {
            Input.Keyboard.ConsumeKeyClick(key);
            hotkey.OnPress();
            return;
        }

        if (hotkey.MouseButton is { } mouse && Input.Mouse.Clicked(mouse)) {
            Input.Mouse.Consume(mouse);
            hotkey.OnPress();
            return;
        }
    }

    private void HandleHoldHotkey(Hotkey hotkey) {
        if (hotkey.Key is { } key && Input.Keyboard.IsKeyHeld(key)) {
            hotkey.OnPress();
            return;
        }

        if (hotkey.MouseButton is { } mouse && Input.Mouse.Held(mouse)) {
            hotkey.OnPress();
            return;
        }
    }

    private void HandleSmoothHotkey(Hotkey hotkey) {
        bool held = false, clicked = false;
        float holdTime = 0f;

        if (hotkey.Key is { } key && Input.Keyboard.HeldOrClicked(key)) {
            held = true;
            clicked = Input.Keyboard.IsKeyClicked(key);
            holdTime = Input.Keyboard.GetHoldTime(key);
        } else if (hotkey.MouseButton is { } mouse && Input.Mouse.HeldOrClicked(mouse)) {
            held = true;
            clicked = Input.Mouse.Clicked(mouse);
            holdTime = Input.Mouse.HeldTime(mouse);
        }

        if (held) {
            if (clicked || (holdTime > 0.2f && RysyEngine.Scene.OnInterval(hotkey.SmoothIntervalTime))) {
                hotkey.OnPress();
                hotkey.SmoothIntervalTime = NextInterval(holdTime);
                return;
            }
        } else {
            hotkey.SmoothIntervalTime = 0;
        }

        double NextInterval(float holdTime) => 0.50 - (holdTime / 2.5f);
    }

    public static bool IsValid(string? hotkeyString) {
        if (hotkeyString is null)
            return false;

        foreach (var hotkey in hotkeyString.Split('|')) {
            foreach (var item in hotkey.Replace(" ", "").Split('+')) {
                var lower = item.ToLowerInvariant();
                if (lower is not ("shift" or "ctrl" or "alt" or "mouseleft" or "mouseright" or "mousemiddle" or (['m', 'o', 'u', 's', 'e', _]))) {
                    if (!Enum.TryParse<Keys>(lower, true, out var key))
                        return false;
                }
            }
        }

        return true;
    }
}

public enum HotkeyModes {
    OnClick, // The hotkey gets triggered the frame you click the button
    OnHold, // The hotkey gets triggered each frame you hold the button
    OnHoldSmoothInterval // The hotkey gets triggered every interval while you hold the button, with the interval decreasing the longer you hold
}

public class Hotkey {
    public bool Ctrl;
    public bool Shift;
    public bool Alt;

    public Keys? Key;
    public int? MouseButton;

    public Action OnPress;
    public HotkeyModes Mode;

    internal double SmoothIntervalTime;

    public Hotkey(string hotkeyString) {
        var inputs = hotkeyString.Replace(" ", "").Split("+");
        foreach (var item in inputs) {
            var lower = item.ToLowerInvariant();
            switch (lower) {
                case "shift":
                    Shift = true;
                    break;
                case "ctrl":
                    Ctrl = true;
                    break;
                case "alt":
                    Alt = true;
                    break;
                case "mouseleft":
                    MouseButton = 0;
                    break;
                case "mouseright":
                    MouseButton = 1;
                    break;
                case "mousemiddle":
                    MouseButton = 2;
                    break;
                case ['m', 'o', 'u', 's', 'e', var button]:
                    MouseButton = int.Parse(button.ToString());
                    break;
                default:
                    if (!Enum.TryParse<Keys>(lower, true, out var key)) {
                        Logger.Write("[Hotkey.ctor]", LogLevel.Error, $"Unknown key: {key} in hotkey: '{hotkeyString}'");
                        return;
                    }

                    Key = key;
                    break;
            }
        }
    }

}
