using Microsoft.Xna.Framework.Graphics;

namespace Rysy.Graphics;

public static class SpriteBatchExtensions
{
    public static void DrawLine(this SpriteBatch b, Vector2 start, float angle, float len, Color color)
    {
        b.Draw(GFX.Pixel, start, null, color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
    }

    public static void DrawLine(this SpriteBatch b, Vector2 start, Vector2 end, Color color)
    {
        var angle = VectorExt.Angle(start, end);

        var len = Vector2.Distance(start, end);
        b.DrawLine(start, angle, len, color);
    }

    public static void DrawCircle(this SpriteBatch b, Vector2 center, float radius, int segments, Color color)
    {
        var angle = 0f;
        var slice = MathF.PI * 2f / segments;
        var start = center + pointOnCircle(0f);

        for (int i = 0; i < segments; i++)
        {
            angle += slice;
            var end = center + pointOnCircle(angle);

            b.DrawLine(start, end, color);

            start = end;
        }

        Vector2 pointOnCircle(float angle) => new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }
}
