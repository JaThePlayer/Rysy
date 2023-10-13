using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Rysy.Extensions;

public static class VectorExt {
    public static Vector2 XY(this Rectangle r) => new(r.X, r.Y);

    /// <summary>
    /// Returns new <see cref="Vector2"/>(<paramref name="r"/>.Width, <paramref name="r"/>.Height)
    /// </summary>
    public static Vector2 WH(this Rectangle r) => new(r.Width, r.Height);

    public static Vector2 Add(this Vector2 v, float x, float y) => new(v.X + x, v.Y + y);
    public static Vector2 AddX(this Vector2 v, float toAdd) => new(v.X + toAdd, v.Y);
    public static Vector2 AddY(this Vector2 v, float toAdd) => new(v.X, v.Y + toAdd);
    public static float Angle(Vector2 from, Vector2 to)
    => float.Atan2(to.Y - from.Y, to.X - from.X);

    public static Vector2 AngleToVector(this float angleRadians, float length)
        => new(float.Cos(angleRadians) * length, float.Sin(angleRadians) * length);

    public static float ToRad(this float angle) => angle / 180f * MathF.PI;

    public static float RadToDegrees(this float angleRadians) => angleRadians * 180f / MathF.PI;

    public static Vector2 Floored(this Vector2 v) =>
#if FNA
        new(float.Floor(v.X), float.Floor(v.Y));
#else
        Vector2.Floor(v);
#endif
    public static Vector2 Rounded(this Vector2 v) =>
#if FNA
        new(float.Round(v.X), float.Round(v.Y));
#else
        Vector2.Round(v);
#endif
    public static Vector2 Normalized(this Vector2 v) => v == default ? default : Vector2.Normalize(v);

    public static Vector2 Snap(this Vector2 v, int gridSize) => (v / gridSize).Floored() * gridSize;

    public static Point GridPosFloor(this Vector2 v, int gridSize) => (v / gridSize).Floored().ToPoint();
    public static Point GridPosRound(this Vector2 v, int gridSize) => (v / gridSize).Rounded().ToPoint();

    public static Vector2 Rotate(this Vector2 v, float rad) {
        float sin = MathF.Sin(rad);
        float cos = MathF.Cos(rad);

        float tx = v.X;
        float ty = v.Y;

        return new(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    public static Vector2 FlipHorizontalAlong(this Vector2 v, Vector2 origin)
        => new(v.X + 2 * (origin.X - v.X), v.Y);

    public static Vector2 FlipVerticalAlong(this Vector2 v, Vector2 origin)
        => new(v.X, v.Y + 2 * (origin.Y - v.Y));

    public static Vector2 RotateAround(this Vector2 v, Vector2 origin, float angleRad) {
        var diff = v - origin;
        var diffRotated = diff.Rotate(angleRad);

        return origin + diffRotated;
    }

    public static Vector2 ToXna(this NumVector2 v) => new(v.X, v.Y);
    public static Vector3 ToXna(this NumVector3 v) => new(v.X, v.Y, v.Z);
    public static Vector4 ToXna(this NumVector4 v) => new(v.X, v.Y, v.Z, v.W);
    
    public static ref NumVector2 AsNumerics(this ref XnaVector2 v) => ref Unsafe.As<XnaVector2, NumVector2>(ref v);
}
