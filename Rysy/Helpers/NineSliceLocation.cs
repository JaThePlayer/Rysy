using ImGuiNET;

namespace Rysy.Helpers;

public enum NineSliceLocation {
    TopLeft, TopMiddle, TopRight,
    Left, Middle, Right,
    BottomLeft, BottomMiddle, BottomRight,

    /// <summary>
    /// An inner corner where the Up-Right location is empty 
    /// </summary>
    InnerCorner_UpRight,
    InnerCorner_UpLeft,
    InnerCorner_DownRight,

    InnerCorner_DownLeft,
}


public static class NineSliceLocationExt {
    public static ImGuiMouseCursor ToMouseCursor(this NineSliceLocation loc) => loc switch {
        NineSliceLocation.TopLeft => ImGuiMouseCursor.ResizeNWSE,
        NineSliceLocation.TopMiddle => ImGuiMouseCursor.ResizeNS,
        NineSliceLocation.TopRight => ImGuiMouseCursor.ResizeNESW,
        NineSliceLocation.Left => ImGuiMouseCursor.ResizeEW,
        NineSliceLocation.Middle => ImGuiMouseCursor.None,
        NineSliceLocation.Right => ImGuiMouseCursor.ResizeEW,
        NineSliceLocation.BottomLeft => ImGuiMouseCursor.ResizeNESW,
        NineSliceLocation.BottomMiddle => ImGuiMouseCursor.ResizeNS,
        NineSliceLocation.BottomRight => ImGuiMouseCursor.ResizeNWSE,
        _ => ImGuiMouseCursor.None,
    };
}