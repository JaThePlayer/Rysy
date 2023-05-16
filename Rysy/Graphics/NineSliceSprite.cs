using Rysy.Extensions;

namespace Rysy.Graphics;

public record struct NineSliceSprite : ISprite {
    public VirtTexture Texture;
    public Rectangle Pos;

    public NineSliceSprite(VirtTexture text) {
        Texture = text;
    }

    public int? Depth { get; set; }
    public Color Color { get; set; }

    public bool IsLoaded => Texture.Texture is { };

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    public void Render() {
        if (Texture.Texture is not { } texture)
            return;

        var b = GFX.Batch;
        const int tileSize = 8;

        var rect = Pos;
        var w = rect.Width;
        var wBy8 = w / tileSize;
        var h = rect.Height;
        var hBy8 = h / tileSize;
        var pos = rect.Location.ToVector2();
        var c = Color;

        
        var texWidth = Texture.Width;
        var texHeight = Texture.Height;

        for (int x = 0; x < wBy8; x++) {
            var left = x == 0;
            var middle = x + 1 < wBy8;

            Rectangle subtext;
            Vector2 offset;

            var subX = left ? 0 : middle ? tileSize : texWidth - tileSize;

            // top
            subtext = Texture.GetSubtextureRect(subX, 0, tileSize, tileSize, out offset);
            b.Draw(texture, pos.AddX(x * tileSize) + offset, subtext, c);

            if (h > 8) {
                // bottom
                subtext = Texture.GetSubtextureRect(subX, texHeight - tileSize, tileSize, tileSize, out offset);
                b.Draw(texture, pos.Add(x * tileSize, h - tileSize) + offset, subtext, c);
            }

            for (int y = 1; y < hBy8 - 1; y++) {
                // middle
                subtext = Texture.GetSubtextureRect(subX, tileSize, tileSize, tileSize, out offset);
                b.Draw(texture, pos.Add(x * tileSize, y * tileSize) + offset, subtext, c);
            }
        }
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (cam is { }) {
            if (!cam.IsRectVisible(Pos.MovedBy(offset)))
                return;
        }

        Render();
    }

    public ISelectionCollider GetCollider()
        => ISelectionCollider.FromRect(Pos);
}

public enum LoopingMode {
    Repeat,

}
