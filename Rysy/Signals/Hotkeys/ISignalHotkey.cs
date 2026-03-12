namespace Rysy.Signals.Hotkeys;

/// <summary>
/// A signal which is fired via a hotkey.
/// Defines the name and default keybind of the hotkey to avoid duplicating this information if the hotkey is registered in multiple places.
/// </summary>
public interface ISignalHotkey : ISignal {
    public static abstract string Name { get; }
    
    public static abstract string DefaultKeybind { get; }
}
