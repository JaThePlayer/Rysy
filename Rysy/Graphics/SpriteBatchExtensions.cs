using Rysy.Extensions;

namespace Rysy.Graphics;

public static class SpriteBatchExtensions {
    public static void DrawLine(this SpriteBatch b, Vector2 start, float angle, float len, Color color, float thickness = 1) {
        b.Draw(Gfx.Pixel, start, null, color, angle, Vector2.Zero, new Vector2(len, thickness), SpriteEffects.None, 0f);
    }

    public static void DrawLine(this SpriteBatch b, Vector2 start, Vector2 end, Color color, float thickness = 1, Vector2 offset = default, float magnitudeOffset = 0f) {
        var angle = VectorExt.Angle(start, end);
        var len = Vector2.Distance(start, end) + magnitudeOffset;

        //var rOffset = new Vector2(offset.X / len, (0.5f + offset.Y) / thickness);
        var rOffset = new Vector2((offset.X) / len, (0.5f + offset.Y) * thickness);

        b.DrawLine(start + rOffset.Floored().Rotate(angle), angle, len, color, thickness);
    }

    public static void DrawCircle(this SpriteBatch b, Vector2 center, float radius, int segments, Color color, float thickness) {
        if (color.A == 0)
            return;

        var angle = 0f;
        var slice = MathF.PI * 2f / segments;
        var start = PointOnCircle(center, radius, 0);

        for (int i = 0; i < segments; i++) {
            angle += slice;
            var end = PointOnCircle(center, radius, angle);

            b.DrawLine(start, end, color, thickness);

            start = end;
        }
    }

    private static Vector2 PointOnCircle(Vector2 center, float radius, float angle) => center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
}
