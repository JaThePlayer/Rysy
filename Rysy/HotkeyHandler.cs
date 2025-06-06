﻿using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Helpers;

namespace Rysy;

public class HotkeyHandler {
    private List<Hotkey> Hotkeys = new();

    /// <summary>
    /// Specifies how this hotkey manager responds to ImGui usage
    /// </summary>
    public ImGuiModes ImGuiMode;

    public enum ImGuiModes {
        /// <summary>
        /// Hotkey always works, even if imgui wants keyboard and mouse
        /// </summary>
        Ignore,
        /// <summary>
        /// Hotkey works only if imgui doesn't want the keyboard
        /// </summary>
        NotWhenUsingKeyboard,
        /// <summary>
        /// Hotkey doesn't work if imgui wants anything
        /// </summary>
        Never,
    }

    public Input Input { get; private set; }

    public LockManager LockManager { get; private set; } = new();

    public HotkeyHandler(Input input, ImGuiModes imguiMode) {
        Input = input;
        ImGuiMode = imguiMode;
    }
    
    public HotkeyHandler AddHistoryHotkeys(Action undo, Action redo, Action save) {
        AddHotkeyFromSettings("undo", "ctrl+z|mouse3", undo, HotkeyModes.OnHoldSmoothInterval, ImGuiModes.NotWhenUsingKeyboard);
        AddHotkeyFromSettings("redo", "ctrl+y|mouse4", redo, HotkeyModes.OnHoldSmoothInterval, ImGuiModes.NotWhenUsingKeyboard);
        AddHotkeyFromSettings("saveMap", "ctrl+s", save);
        
        return this;
    }

    /// <summary>
    /// Adds a new hotkey, loading it from settings using <paramref name="name"/>, saving the hotkey to the settings file using <paramref name="defaultKeybind"/> if it doesn't exist.
    /// </summary>
    public HotkeyHandler AddHotkeyFromSettings(string name, string defaultKeybind, Action onPress, HotkeyModes mode = HotkeyModes.OnClick, ImGuiModes imguiMode = ImGuiModes.Ignore) {
        return AddHotkey(Settings.Instance.GetOrCreateHotkey(name, defaultKeybind), onPress, mode, imguiMode);
    }

    /// <summary>
    /// Adds a new hotkey, parsed from <paramref name="hotkeyString"/>.
    /// </summary>
    /// <returns>this</returns>
    public HotkeyHandler AddHotkey(string hotkeyString, Action onPress, HotkeyModes mode = HotkeyModes.OnClick, ImGuiModes imguiMode = ImGuiModes.Ignore) {
        if (hotkeyString is null)
            return this;

        foreach (var splitHotkey in hotkeyString.Split('|')) {
            var hotkey = new Hotkey(splitHotkey) {
                OnPress = onPress,
                Mode = mode,
                ImGuiMode = imguiMode,
            };

            lock (Hotkeys)
                Hotkeys.Add(hotkey);
        }

        return this;
    }

    private static bool ImGuiModeMet(ImGuiModes mode) {
        return mode switch {
            ImGuiModes.NotWhenUsingKeyboard => !ImGuiManager.WantTextInput,
            ImGuiModes.Never => !ImGuiManager.WantTextInput && !ImGuiManager.WantCaptureMouse,
            _ => true,
        };
    }
    
    public void Update() {
        if (!ImGuiModeMet(ImGuiMode))
            return;

        if (LockManager.IsLocked())
            return;
        
        var ctrl = Input.Keyboard.Ctrl();
        var shift = Input.Keyboard.Shift();
        var alt = Input.Keyboard.Alt();
        lock (Hotkeys)
            foreach (var hotkey in Hotkeys) {
                if (!ImGuiModeMet(hotkey.ImGuiMode))
                    return;
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
        
        if (hotkey.ScrollUp && Input.Mouse.ScrollDelta > 0) {
            hotkey.OnPress();
            return;
        }
        
        if (hotkey.ScrollDown && Input.Mouse.ScrollDelta < 0) {
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
        
        if (hotkey.ScrollUp && Input.Mouse.ScrollDelta > 0) {
            hotkey.OnPress();
            return;
        }
        
        if (hotkey.ScrollDown && Input.Mouse.ScrollDelta < 0) {
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
        } else if (hotkey.ScrollUp && Input.Mouse.ScrollDelta > 0) {
            held = true;
            clicked = true;
        } else if (hotkey.ScrollDown && Input.Mouse.ScrollDelta < 0) {
            held = true;
            clicked = true;
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

    public static ValidationResult IsValid(string? hotkeyString) {
        if (hotkeyString is "")
            return ValidationResult.Ok;
        
        if (hotkeyString is null)
            return ValidationResult.CantBeNull;

        foreach (var hotkey in hotkeyString.Split('|')) {
            foreach (var item in hotkey.Replace(" ", "", StringComparison.Ordinal).Split('+')) {
                var lower = item.ToLowerInvariant();
                if (lower is not ("shift" or "ctrl" or "alt" or "mouseleft" or "mouseright" or "mousemiddle" or "scrollup" or "scrolldown" or (['m', 'o', 'u', 's', 'e', _]))) {
                    if (!Enum.TryParse<Keys>(lower, true, out var key))
                        return ValidationResult.GenericError;
                }
            }
        }

        return ValidationResult.Ok;
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
    public bool ScrollDown;
    public bool ScrollUp;

    public Action OnPress;
    public HotkeyModes Mode;

    public HotkeyHandler.ImGuiModes ImGuiMode = HotkeyHandler.ImGuiModes.Ignore;

    internal double SmoothIntervalTime;

    public Hotkey(string hotkeyString) {
        if (hotkeyString.IsNullOrWhitespace())
            return;
        
        var inputs = hotkeyString.Replace(" ", "", StringComparison.Ordinal).Split("+");
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
                case "scrollup":
                    ScrollUp = true;
                    break;
                case "scrolldown":
                    ScrollDown = true;
                    break;
                case ['m', 'o', 'u', 's', 'e', var button]:
                    MouseButton = int.Parse(button.ToString(), CultureInfo.InvariantCulture);
                    break;
                default:
                    if (!Enum.TryParse<Keys>(lower, true, out var key)) {
                        Logger.Write("Hotkey.ctor", LogLevel.Error, $"Unknown key: {lower} in hotkey: '{hotkeyString}'");
                        return;
                    }

                    Key = key;
                    break;
            }
        }
    }

}
