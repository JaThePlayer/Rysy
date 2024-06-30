namespace Rysy.Tools;

/// <summary>
/// Provides implementations for some hotkeys on selections,
/// making sure hotkeys used by multiple tools are defined the same way in all of them
/// </summary>
public interface ISelectionHotkeyTool {
    public void Flip(bool vertical);

    public void Rotate(RotationDirection direction);
}

public static class SelectionHotkeysExt {
    public static void AddSelectionHotkeys(this ISelectionHotkeyTool tool, HotkeyHandler handler) {
        handler.AddHotkeyFromSettings("selection.flipHorizontal", "h", () => tool.Flip(false));
        handler.AddHotkeyFromSettings("selection.flipVertical",   "v", () => tool.Flip(true));
        handler.AddHotkeyFromSettings("selection.rotateRight",    "r", () => tool.Rotate(RotationDirection.Right));
        handler.AddHotkeyFromSettings("selection.rotateLeft",     "l", () => tool.Rotate(RotationDirection.Left));
    }
}