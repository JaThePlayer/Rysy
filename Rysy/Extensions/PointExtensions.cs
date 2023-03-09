namespace Rysy;

public static class PointExtensions {
    public static Point Negate(this Point point) => new(-point.X, -point.Y);
}
