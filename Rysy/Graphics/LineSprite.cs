﻿using Microsoft.Xna.Framework.Graphics;

namespace Rysy.Graphics;

public record struct LineSprite : ISprite
{
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public float Alpha
    {
        get => Color.A / 255f;
        set
        {
            Color = new Color(Color, value);
        }
    }

    public bool IsLoaded => true;

    public Vector2[] Positions;

    public int Thickness = 1;

    public LineSprite(Vector2[] positions)
    {
        Positions = positions;
    }

    public void Render()
    {
        var b = GFX.Batch;
        for (int i = 0; i < Positions.Length - 1; i++)
        {
            var start = Positions[i];
            var end = Positions[i + 1];
            var angle = Extensions.Angle(start, end);

            var len = Vector2.Distance(start, end);

            b.Draw(GFX.Pixel, start, null, Color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
        }
    }
}
