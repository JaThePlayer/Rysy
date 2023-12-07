namespace Rysy.Extensions;

public static class PointExtensions {
    public static Point Negate(this Point point) => new(-point.X, -point.Y);

    public static Vector2 ToNVector2(this Point point) => new Vector2(point.X, point.Y);

    public static Point Mult(this Point p, int a) => new(p.X * a, p.Y * a);
}
