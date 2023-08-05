using Rysy.Extensions;
using Rysy.Selections;

namespace Rysy.Graphics;

public record struct RectangleSprite : ISprite {
    public int? Depth { get; set; }

    public Rectangle Pos;

    public Color Color { get; set; } = Color.White;
    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
            OutlineColor = OutlineColor * alpha,
        };
    }

    public bool IsLoaded => true;

    public Color OutlineColor = Color.Transparent;
    public int OutlineWidth = 1;

    public RectangleSprite() {
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (cam is { }) {
            if (!cam.IsRectVisible(Pos.MovedBy(offset)))
                return;
        }

        Render();
    }

    public void Render() {
        var outline = OutlineColor;
        var color = Color;

        if (outline != Color.Transparent) {
            var outlineWidth = OutlineWidth;
            var w = Pos.Width;
            var h = Pos.Height;
            var left = Pos.X;
            var right = Pos.X + w - outlineWidth;
            var top = Pos.Y;
            var bottom = Pos.Y + h - outlineWidth;


            if (outline.A != byte.MaxValue || color.A != byte.MaxValue) {
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, top, w, outlineWidth), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, bottom, w, outlineWidth), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(left, top + outlineWidth, outlineWidth, h - (outlineWidth * 2)), null, outline);
                GFX.Batch.Draw(GFX.Pixel, new Rectangle(right, top + outlineWidth, outlineWidth, h - (outlineWidth * 2)), null, outline);
            } else {
                // if the colors are fully opaque, we can just render one big rectangle, since the smaller inner rectangle will fully cover up the overlapping parts
                GFX.Batch.Draw(GFX.Pixel, Pos, null, outline);
            }

            GFX.Batch.Draw(GFX.Pixel, new Rectangle(left + outlineWidth, top + outlineWidth, w - (outlineWidth * 2), h - (outlineWidth * 2)), null, color);
        } else {
            GFX.Batch.Draw(GFX.Pixel, Pos, null, color);
        }
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromRect(Pos);
}
