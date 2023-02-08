namespace Rysy.Helpers;
public static class CelesteEnums {
    public static Color[] RoomColors = new Color[] {
        Color.White,
        "f6735e".FromRGB(),
        "85f65e".FromRGB(),
        "37d7e3".FromRGB(),
        "376be3".FromRGB(),
        "c337e3".FromRGB(),
        "e33773".FromRGB()
    };

    public static string[] RoomColorNames = new string[] {
        "White",
        "Orange",
        "Green",
        "Light Blue",
        "Blue",
        "Purple",
        "Red"
    };
}

public enum WindPatterns {
    None,
    Left,
    Right,
    LeftStrong,
    RightStrong,
    LeftOnOff,
    RightOnOff,
    LeftOnOffFast,
    RightOnOffFast,
    Alternating,
    LeftGemsOnly,
    RightCrazy,
    Down,
    Up,
    Space
}