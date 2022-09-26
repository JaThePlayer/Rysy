namespace Rysy.Graphics;

public record struct RectangleSprite : ISprite
{
    public int? Depth { get; set; }

    public Rectangle Pos;

    public Color Color { get; set; } = Color.White;
    public float Alpha { 
        get => Color.A / 255f; 
        set {
            OutlineColor *= value;
            Color *= value;
        }
    }

    public bool IsLoaded => true;

    public Color OutlineColor = Color.Transparent;

    public RectangleSprite()
    {
    }

    public void Render()
    {
        var outline = OutlineColor;
        var color = Color;

        if (outline != Color.Transparent)
        {
            var w = Pos.Width;
            var h = Pos.Height;
            var left = Pos.X;
            var right = Pos.X + w - 1;
            var top = Pos.Y;
            var bottom = Pos.Y + h - 1;


            if (outline.A != byte.MaxValue || color.A != byte.MaxValue)
            {
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left,  top,     w, 1    ), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left,  bottom,  w, 1    ), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left,  top + 1, 1, h - 2), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(right, top + 1, 1, h - 2), null, outline);
            } else
            {
                // if the colors are fully opaque, we can just render one big rectangle, since the smaller inner rectangle will fully cover up the overlapping parts
                GFX.Batch.Draw(GFX.Pixel, Pos, null, outline);
            }
            
            GFX.Batch.Draw(GFX.Pixel, new Rectangle(left + 1, top + 1, w - 2, h - 2), null, color);
        } else
        {
            GFX.Batch.Draw(GFX.Pixel, Pos, null, color);
        }

    }
}
