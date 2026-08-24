namespace Rysy.Tools;

/// <summary>
/// Provides implementations for some hotkeys on selections,
/// making sure hotkeys used by multiple tools are defined the same way in all of them
/// </summary>
internal interface ISelectionHotkeyTool {
    public void Flip(bool vertical, bool area);

    public void Rotate(RotationDirection direction);

    public void AddNode(Vector2? at);

    public void SwapDecalLayer();
}

internal static class SelectionHotkeysExt {
    internal static void AddSelectionHotkeys<T>(this T tool, HotkeyHandler handler)
    where T : Tool, ISelectionHotkeyTool {
        handler.AddHotkeyFromSettings("selection.flipHorizontal", "h", () => tool.Flip(false, area: false));
        handler.AddHotkeyFromSettings("selection.flipVertical",   "v", () => tool.Flip(true, area: false));
        handler.AddHotkeyFromSettings("selection.flipHorizontalArea", "shift+h", () => tool.Flip(false, area: true));
        handler.AddHotkeyFromSettings("selection.flipVerticalArea",   "shift+v", () => tool.Flip(true, area: true));
        handler.AddHotkeyFromSettings("selection.rotateRight",    "r", () => tool.Rotate(RotationDirection.Right));
        handler.AddHotkeyFromSettings("selection.rotateLeft",     "l", () => tool.Rotate(RotationDirection.Left));
        
        handler.AddHotkeyFromSettings("selection.addNode", "shift+n", () => tool.AddNode(at: null));
        handler.AddHotkeyFromSettings("selection.addNodeAtMouse", "n", 
            () => tool.AddNode(at: tool.GetMouseRoomPos(tool.EditorState.Camera, tool.EditorState.CurrentRoom).ToVector2().Snap(8)));
        
        handler.AddHotkeyFromSettings("selection.swapDecalLayer", "ctrl+d", tool.SwapDecalLayer);
    }
}