using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Rysy.Extensions;

public static class VectorExt {
    extension(Rectangle r)
    {
        public Vector2 Xy() => new(r.X, r.Y);

        /// <summary>
        /// Returns new <see cref="Vector2"/>(<paramref name="r"/>.Width, <paramref name="r"/>.Height)
        /// </summary>
        public Vector2 Wh() => new(r.Width, r.Height);
    }

    extension(Vector2 v)
    {
        public Vector2 Add(float x, float y) => new(v.X + x, v.Y + y);
        public Vector2 AddX(float toAdd) => new(v.X + toAdd, v.Y);
        public Vector2 AddY(float toAdd) => new(v.X, v.Y + toAdd);

        public float Angle()
        {
            return (float)Math.Atan2(v.Y, v.X);
        }
    }

    public static float Angle(Vector2 from, Vector2 to)
    => float.Atan2(to.Y - from.Y, to.X - from.X);

    extension(float angleRadians)
    {
        public Vector2 AngleToVector(float length)
            => new(float.Cos(angleRadians) * length, float.Sin(angleRadians) * length);

        public float ToRad() => angleRadians / 180f * MathF.PI;
        public float RadToDegrees() => angleRadians * 180f / MathF.PI;
    }

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
    extension(Vector2 v)
    {
        public Vector2 Normalized() => v == default ? default : Vector2.Normalize(v);
        public Vector2 Snap(int gridSize) => (v / gridSize).Floored() * gridSize;
        public Vector2 SnapRound(int gridSize) => (v / gridSize).Rounded() * gridSize;
        public Point GridPosFloor(int gridSize) => (v / gridSize).Floored().ToPoint();
        public Point GridPosRound(int gridSize) => (v / gridSize).Rounded().ToPoint();

        public Vector2 Rotate(float rad) {
            float sin = MathF.Sin(rad);
            float cos = MathF.Cos(rad);

            float tx = v.X;
            float ty = v.Y;

            return new(cos * tx - sin * ty, sin * tx + cos * ty);
        }

        public Vector2 FlipHorizontalAlong(Vector2 origin)
            => new(v.X + 2 * (origin.X - v.X), v.Y);

        public Vector2 FlipVerticalAlong(Vector2 origin)
            => new(v.X, v.Y + 2 * (origin.Y - v.Y));

        public Vector2 RotateAround(Vector2 origin, float angleRad) {
            var diff = v - origin;
            var diffRotated = diff.Rotate(angleRad);

            return origin + diffRotated;
        }

        public Vector2 RotateTowards(float targetAngleRadians, float maxMoveRadians)
        {
            return AngleToVector(AngleApproach(v.Angle(), targetAngleRadians, maxMoveRadians), v.Length());
        }
    }

    public static Vector2 ToXna(this NumVector2 v) => new(v.X, v.Y);
    public static Vector3 ToXna(this NumVector3 v) => new(v.X, v.Y, v.Z);
    public static Vector4 ToXna(this NumVector4 v) => new(v.X, v.Y, v.Z, v.W);
    
    public static ref NumVector2 AsNumerics(this ref XnaVector2 v) => ref Unsafe.As<XnaVector2, NumVector2>(ref v);
    
    public static float AngleApproach(float val, float target, float maxMove)
    {
        float value = AngleDiff(val, target);
        if (Math.Abs(value) < maxMove)
        {
            return target;
        }
        return val + MathHelper.Clamp(value, 0f - maxMove, maxMove);
    }
    
    public static float AngleDiff(float radiansA, float radiansB)
    {
        float num;
        for (num = radiansB - radiansA; num > (float)Math.PI; num -= (float)Math.PI * 2f)
        {
        }
        for (; num <= -(float)Math.PI; num += (float)Math.PI * 2f)
        {
        }
        return num;
    }
}
