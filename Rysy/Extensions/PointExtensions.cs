namespace Rysy.Extensions;

public static class PointExtensions {
    extension(Point point)
    {
        public Point Negate() => new(-point.X, -point.Y);
        public Vector2 ToNVector2() => new Vector2(point.X, point.Y);
        public Point Mult(int a) => new(point.X * a, point.Y * a);
    }
}
