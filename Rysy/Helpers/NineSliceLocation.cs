using Hexa.NET.ImGui;

namespace Rysy.Helpers;

public enum NineSliceLocation {
    TopLeft, TopMiddle, TopRight,
    Left, Middle, Right,
    BottomLeft, BottomMiddle, BottomRight,

    /// <summary>
    /// An inner corner where the Up-Right location is empty 
    /// </summary>
    InnerCornerUpRight,
    InnerCornerUpLeft,
    InnerCornerDownRight,

    InnerCornerDownLeft,
}


public static class NineSliceLocationExt {
    public static ImGuiMouseCursor ToMouseCursor(this NineSliceLocation loc) => loc switch {
        NineSliceLocation.TopLeft => ImGuiMouseCursor.ResizeNwse,
        NineSliceLocation.TopMiddle => ImGuiMouseCursor.ResizeNs,
        NineSliceLocation.TopRight => ImGuiMouseCursor.ResizeNesw,
        NineSliceLocation.Left => ImGuiMouseCursor.ResizeEw,
        NineSliceLocation.Middle => ImGuiMouseCursor.None,
        NineSliceLocation.Right => ImGuiMouseCursor.ResizeEw,
        NineSliceLocation.BottomLeft => ImGuiMouseCursor.ResizeNesw,
        NineSliceLocation.BottomMiddle => ImGuiMouseCursor.ResizeNs,
        NineSliceLocation.BottomRight => ImGuiMouseCursor.ResizeNwse,
        _ => ImGuiMouseCursor.None,
    };
}