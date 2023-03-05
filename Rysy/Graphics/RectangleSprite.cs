namespace Rysy.Graphics;

public record struct RectangleSprite : ISprite {
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

    public RectangleSprite() {
    }

    public void Render() {
        var outline = OutlineColor;
        var color = Color;

        if (outline != Color.Transparent) {
            const int OutlineWidth = 1;
            var w = Pos.Width;
            var h = Pos.Height;
            var left = Pos.X;
            var right = Pos.X + w - OutlineWidth;
            var top = Pos.Y;
            var bottom = Pos.Y + h - OutlineWidth;


            if (outline.A != byte.MaxValue || color.A != byte.MaxValue) {
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, top, w, OutlineWidth), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, bottom, w, OutlineWidth), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, top + OutlineWidth, OutlineWidth, h - (OutlineWidth * 2)), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(right, top + OutlineWidth, OutlineWidth, h - (OutlineWidth * 2)), null, outline);
            } else {
                // if the colors are fully opaque, we can just render one big rectangle, since the smaller inner rectangle will fully cover up the overlapping parts
                GFX.Batch.Draw(GFX.Pixel, Pos, null, outline);
            }

            GFX.Batch.Draw(GFX.Pixel, new Rectangle(left + OutlineWidth, top + OutlineWidth, w - (OutlineWidth * 2), h - (OutlineWidth * 2)), null, color);
        } else {
            GFX.Batch.Draw(GFX.Pixel, Pos, null, color);
        }

    }
}
