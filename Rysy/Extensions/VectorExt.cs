namespace Rysy;

public static class VectorExt
{
    public static Vector2 XY(this Rectangle r) => new(r.X, r.Y);

    /// <summary>
    /// Returns new <see cref="Vector2"/>(<paramref name="r"/>.Width, <paramref name="r"/>.Height)
    /// </summary>
    public static Vector2 WH(this Rectangle r) => new(r.Width, r.Height);

    public static Vector2 AddX(this Vector2 v, float toAdd) => new(v.X + toAdd, v.Y);
    public static Vector2 AddY(this Vector2 v, float toAdd) => new(v.X, v.Y + toAdd);
    public static float Angle(Vector2 from, Vector2 to)
    => float.Atan2(to.Y - from.Y, to.X - from.X);

    public static Vector2 AngleToVector(this float angleRadians, float length)
        => new(float.Cos(angleRadians) * length, float.Sin(angleRadians) * length);

    public static float ToRad(this float angle) => angle / 180f * MathF.PI;

    public static Vector2 Floored(this Vector2 v) => Vector2.Floor(v);
    public static Vector2 Rounded(this Vector2 v) => Vector2.Round(v);
    public static Vector2 Normalized(this Vector2 v) => Vector2.Normalize(v);

    public static Vector2 Snap(this Vector2 v, int gridSize) => (v / gridSize).Floored() * gridSize;

    public static Point GridPos(this Vector2 v, int gridSize) => (v / gridSize).Floored().ToPoint();

}
