namespace Rysy.Signals.Hotkeys;

public record struct HotkeyCloseWindowAndSave : ISignalHotkey {
    public static string Name => "exit_window_and_save";
    public static string DefaultKeybind => "esc";
}

public record struct HotkeyCloseWindow : ISignalHotkey {
    public static string Name => "exit_window";
    public static string DefaultKeybind => "shift+esc";
}
