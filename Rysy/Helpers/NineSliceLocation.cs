namespace Rysy.Helpers;

public enum NineSliceLocation {
    TopLeft,    TopMiddle,    TopRight,
    Left,       Middle,       Right,
    BottomLeft, BottomMiddle, BottomRight, 

    /// <summary>
    /// An inner corner where the Up-Right location is empty 
    /// </summary>
    InnerCorner_UpRight,
    InnerCorner_UpLeft,
    InnerCorner_DownRight,

    InnerCorner_DownLeft,
}
