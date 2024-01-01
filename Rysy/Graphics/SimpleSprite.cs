using Rysy.Selections;

namespace Rysy.Graphics;

/// <summary>
/// A trimmed-down version of <see cref="Sprite"/> with much less features, which reduces its size, giving better performance.
/// </summary>
public record struct SimpleSprite : ITextureSprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    
    public Vector2 Pos { get; set; }
    public VirtTexture Texture;
    public Vector2 Origin;
    private bool Prepared;
    
    public SimpleSprite(VirtTexture text) {
        Texture = text;
    }
    
    private int Width => Texture.Width;
    private int Height => Texture.Height;

    private Rectangle ClipRect => Texture.ClipRect;
    
    public ISprite WithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha,
    };

    public bool IsLoaded => Texture.Texture is { };

    private void CacheFields() {
        if (!Prepared) {
            Prepared = true;

            // Fixup properties now, at this point nothing should try to get stuff from the sprite...

            var drawOffset = Texture.DrawOffset;
            // sprites with dimensions not divible by 2 would get rendered at half pixel offsets while centering...
            var nonDivisibleBy2 = new Vector2(Width % 2, Height % 2);
            if (nonDivisibleBy2 != default)
                drawOffset += (nonDivisibleBy2 * Origin);

            Origin = (Origin * new Vector2(Width, Height)) + drawOffset;
        }
    }
    
    public void Render(SpriteRenderCtx ctx) {
        if (Texture.Texture is { } texture) {
            // store some fields for later use
            // this is not done in the constructor, as that would force preloading
            CacheFields();

            var origin = Origin;
            var pos = Pos;
            var color = Color;

            if (ctx.Camera is { } cam) {
                var rPos = Pos - origin;
                if (!cam.IsRectVisible(rPos + ctx.CameraOffset, Width, Height))
                    return;
            }

            if (color != default) {
                GFX.Batch.Draw(texture, pos, Texture.ClipRect, color, 0f, origin, Vector2.One, SpriteEffects.None, 0f);
            }
        }
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);
    
    public Rectangle? GetRenderRect() {
        if (Texture.Texture is not { } texture) {
            return null;
        }

        CacheFields();

        Vector2 pos;
        pos = Pos - Origin;
        
        return new Rectangle((int) pos.X, (int) pos.Y, ClipRect.Width, ClipRect.Height);
    }
    
    public SimpleSprite Centered() => this with {
        Origin = new(.5f, .5f)
    };
    
    public SimpleSprite WithTexture(VirtTexture text) => this with { Texture = text };

    /// <summary>
    /// Creates a new sprite which uses a outline texture.
    /// This tints a pitch-white sprite.
    /// </summary>
    public SimpleSprite WithOutlineTexture() => WithTexture(Texture.GetOutlineTexture());

    public Sprite ToSprite() => new(Texture) {
        Pos = Pos,
        Color = Color,
        Origin = Origin,
        Depth = Depth,
    };
}