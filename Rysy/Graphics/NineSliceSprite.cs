using Rysy.Selections;

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

    public LoopingModes BorderMode { get; set; } = LoopingModes.Repeat;
    public LoopingModes FillMode { get; set; } = LoopingModes.Repeat;

    public RenderModes RenderMode { get; set; } = RenderModes.Fill;

    public int TileWidth { get; set; } = 8;
    
    public int TileHeight { get; set; } = 8;
    
    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    private static int GetMiddleSubtextureX(LoopingModes mode, Vector2 pos, int xIdx, int textureWidth, int tileWidth) {
        var availableTileCount = (textureWidth / tileWidth) - 2;
        return availableTileCount switch {
            < 1 => 0, // Oh well
            1 => tileWidth,
            _ => mode switch {
                LoopingModes.Random => pos.SeededRandomInclusive(1, availableTileCount) * tileWidth,
                _ => ((xIdx - 1) % (availableTileCount) + 1) * tileWidth,
            }
        };
    }

    public void Render() {
        if (Texture.Texture is not { } texture)
            return;

        var b = GFX.Batch;

        var rect = Pos;
        var w = rect.Width;
        var wBy8 = w / TileWidth;
        var h = rect.Height;
        var hBy8 = h / TileHeight;
        var pos = rect.Location.ToVector2();
        var c = Color;
        
        var texWidth = Texture.Width;
        var texHeight = Texture.Height;

        if (texWidth <= 0 || texHeight <= 0)
            return;
        
        Rectangle subtext;
        Vector2 offset;
        
        // Corners
        subtext = Texture.GetSubtextureRect(0, 0, TileWidth, TileHeight, out offset);
        b.Draw(texture, pos + offset, subtext, c);
        subtext = Texture.GetSubtextureRect(texWidth - TileWidth, 0, TileWidth, TileHeight, out offset);
        b.Draw(texture, pos.AddX(w - TileWidth) + offset, subtext, c);
        subtext = Texture.GetSubtextureRect(texWidth - TileWidth, texHeight - TileHeight, TileWidth, TileHeight, out offset);
        b.Draw(texture, pos.AddX(w - TileWidth).AddY(h - TileHeight) + offset, subtext, c);
        subtext = Texture.GetSubtextureRect(0, texHeight - TileHeight, TileWidth, TileHeight, out offset);
        b.Draw(texture, pos.AddY(h - TileHeight) + offset, subtext, c);
        
        // Top and Bottom
        for (int x = 1; x < wBy8 - 1; x++) {
            // top
            var renderPos = pos.AddX(x * TileWidth);
            subtext = Texture.GetSubtextureRect(GetMiddleSubtextureX(BorderMode, renderPos, x, texWidth, TileWidth), 0, TileWidth, TileHeight, out offset);
            b.Draw(texture, renderPos + offset, subtext, c);
            
            // bottom
            if (h > 8) {
                renderPos = pos.Add(x * TileWidth, h - TileHeight);
                subtext = Texture.GetSubtextureRect(GetMiddleSubtextureX(BorderMode, renderPos, x, texWidth, TileWidth), texHeight - TileHeight, TileWidth, TileHeight, out offset);
                b.Draw(texture, renderPos + offset, subtext, c);
            }
        }
        
        // Left and Right
        for (int y = 1; y < hBy8 - 1; y++) {
            // left
            var renderPos = pos.AddY(y * TileHeight);
            subtext = Texture.GetSubtextureRect(0, GetMiddleSubtextureX(BorderMode, renderPos, y, texHeight, TileHeight), TileWidth, TileHeight, out offset);
            b.Draw(texture, renderPos + offset, subtext, c);
            
            // right
            if (w > 8) {
                renderPos = pos.Add(w - TileWidth, y * TileWidth);
                subtext = Texture.GetSubtextureRect(texWidth - TileWidth, GetMiddleSubtextureX(BorderMode, renderPos, y, texHeight, TileHeight), TileWidth, TileHeight, out offset);
                b.Draw(texture, renderPos + offset, subtext, c);
            }
        }

        if (RenderMode != RenderModes.Border) {
            // Middle
            for (int x = 1; x < wBy8 - 1; x++) {
                for (int y = 1; y < hBy8 - 1; y++) {
                    var renderPos = pos.Add(x * TileWidth, y * TileWidth);
                    subtext = Texture.GetSubtextureRect(GetMiddleSubtextureX(BorderMode, renderPos, x, texWidth, TileWidth), GetMiddleSubtextureX(BorderMode, renderPos,  y,texHeight, TileHeight), TileWidth, TileHeight, out offset);
                    b.Draw(texture, renderPos + offset, subtext, c);
                }
            }
        }
    }

    public void Render(SpriteRenderCtx ctx) {
        if (ctx.Camera is { } cam) {
            if (!cam.IsRectVisible(Pos.MovedBy(ctx.CameraOffset)))
                return;
        }

        Render();
    }

    public ISelectionCollider GetCollider()
        => ISelectionCollider.FromRect(Pos);
    
    public enum LoopingModes {
        Repeat,
        Random,
    }

    public enum RenderModes {
        /// <summary>
        /// Renders both the border and the inside of the entity, default.
        /// </summary>
        Fill,
        /// <summary>
        /// Only renders the border around the sprite, leaving the center empty
        /// </summary>
        Border,
    }
}
