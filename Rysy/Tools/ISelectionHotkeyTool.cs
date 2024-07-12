namespace Rysy.Tools;

/// <summary>
/// Provides implementations for some hotkeys on selections,
/// making sure hotkeys used by multiple tools are defined the same way in all of them
/// </summary>
internal interface ISelectionHotkeyTool {
    public void Flip(bool vertical);

    public void Rotate(RotationDirection direction);

    public void AddNode(Vector2? at);
}

internal static class SelectionHotkeysExt {
    internal static void AddSelectionHotkeys<T>(this T tool, HotkeyHandler handler)
    where T : Tool, ISelectionHotkeyTool {
        handler.AddHotkeyFromSettings("selection.flipHorizontal", "h", () => tool.Flip(false));
        handler.AddHotkeyFromSettings("selection.flipVertical",   "v", () => tool.Flip(true));
        handler.AddHotkeyFromSettings("selection.rotateRight",    "r", () => tool.Rotate(RotationDirection.Right));
        handler.AddHotkeyFromSettings("selection.rotateLeft",     "l", () => tool.Rotate(RotationDirection.Left));
        
        handler.AddHotkeyFromSettings("selection.addNode", "shift+n", () => tool.AddNode(at: null));
        handler.AddHotkeyFromSettings("selection.addNodeAtMouse", "n", () => tool.AddNode(at: tool.GetMouseRoomPos(EditorState.Camera, EditorState.CurrentRoom!).ToVector2().Snap(8)));
    }
}