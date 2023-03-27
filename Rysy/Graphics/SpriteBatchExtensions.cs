using Rysy.Extensions;

namespace Rysy.Graphics;

public static class SpriteBatchExtensions {
    public static void DrawLine(this SpriteBatch b, Vector2 start, float angle, float len, Color color, float thickness = 1) {
        b.Draw(GFX.Pixel, start, null, color, angle, Vector2.Zero, new Vector2(len, thickness), SpriteEffects.None, 0f);
    }

    public static void DrawLine(this SpriteBatch b, Vector2 start, Vector2 end, Color color, float thickness = 1, Vector2 offset = default, float magnitudeOffset = 0f) {
        var angle = VectorExt.Angle(start, end);
        var len = Vector2.Distance(start, end) + magnitudeOffset;

        var rOffset = new Vector2(offset.X / len, (0.5f + offset.Y) / thickness);

        b.DrawLine(start + rOffset, angle, len, color, thickness);
    }

    public static void DrawCircle(this SpriteBatch b, Vector2 center, float radius, int segments, Color color) {
        var angle = 0f;
        var slice = MathF.PI * 2f / segments;
        var start = center + pointOnCircle(0f);

        for (int i = 0; i < segments; i++) {
            angle += slice;
            var end = center + pointOnCircle(angle);

            b.DrawLine(start, end, color);

            start = end;
        }

        Vector2 pointOnCircle(float angle) => new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }
}
