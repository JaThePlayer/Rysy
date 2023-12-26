using Rysy.Extensions;
using System.Runtime.CompilerServices;

namespace Rysy.Graphics;

/// <summary>
/// Represents a template that can be used to construct <see cref="TemplatedSprite"/> or <see cref="TemplatedOutlinedSprite"/> instances.
/// Used to reduce memory footprint of sprites and improve performance.
/// </summary>
public sealed record SpriteTemplate {
    public static SpriteTemplate FromTexture(string path, int depth) {
        var t = GFX.Atlas[path];

        return new() {
            Texture = t,
            DrawOffset = t.DrawOffset,
            Depth = depth,
        };
    }

    public SpriteTemplate WithTexture(VirtTexture newTex) => this with {
        Texture = newTex,
        DrawOffset = newTex.DrawOffset
    };

    public SpriteTemplate WithOutlineTexture() => WithTexture(Texture.GetOutlineTexture());
    
    public SpriteTemplate WithDepth(int d) => this with {
        Depth = d
    };
    
    public SpriteTemplate Centered() => this with { Origin = new(0.5f) };

    public TemplatedSprite Create(Vector2 pos, Color color)
        => new(this) { Pos = pos, Color = color };
    public TemplatedOutlinedSprite CreateOutlined(Vector2 pos, Color color, Color outlineColor)
        => new(this) { Pos = pos, Color = color, OutlineColor = outlineColor };

    public ColoredSpriteTemplate CreateColoredTemplate(Color color) =>
        new(this, color, default);
    
    public ColoredSpriteTemplate CreateColoredTemplate(Color color, Color outlineColor) =>
        new(this, color, outlineColor);
    
    public int Depth { get; set; }
    public VirtTexture Texture { get; private set; }
    public Rectangle? ClipRect { get; private set; }
    
    public Vector2 Origin { get; set; }
    public float Rotation { get; set; } = 0f;
    public Vector2 Scale { get; set; } = Vector2.One;

    public bool IsLoaded => Texture.Texture is { };
    
    private Vector2 DrawOffset;
    private int Width;
    private int Height;
    private Vector2 SubtextureOffset;
    
    private Vector2 _multOrigin;
    private bool _prepared;
    private SpriteEffects Flip => SpriteEffects.None;

    public void RenderAt(Camera? cam, Vector2 offset, Vector2 pos, Color color, Color outlineColor) {
        if (Texture.Texture is not { } texture)
            return;
        
        // store some fields for later use
        // this is not done in the constructor, as that would force preloading
        CacheFields();

        var scale = Scale;
        var origin = _multOrigin;

        // todo: figure out if calculating rotated rectangles for culling is worth it
        if (cam is { } && Rotation == 0f) {
            var size = new Vector2(Width * scale.X, Height * scale.Y);
            var rPos = pos - origin * scale;
            if (!cam.IsRectVisible(rPos + offset, (int) size.X, (int) size.Y))
                return;
        }

        var flip = Flip;
        pos += SubtextureOffset.Rotate(Rotation);

        if (outlineColor != default) {
            Render(texture, pos + new Vector2(-1f, 0f), outlineColor, scale, flip, origin);
            Render(texture, pos + new Vector2(1f, 0f), outlineColor, scale, flip, origin);
            Render(texture, pos + new Vector2(0f, 1f), outlineColor, scale, flip, origin);
            Render(texture, pos + new Vector2(0f, -1f), outlineColor, scale, flip, origin);
        }

        if (color != default)
            Render(texture, pos, color, scale, flip, origin);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Render(Texture2D texture, Vector2 pos, Color color, Vector2 scale, SpriteEffects flip, Vector2 origin) {
        GFX.Batch.Draw(texture, pos, ClipRect, color, Rotation, origin, scale, flip, 0f);
    }
    
    private void LoadSizeFromTexture() {
        Width = Texture.Width;
        Height = Texture.Height;
    }
    
    public Rectangle? GetRenderRect(Vector2 atPos) {
        if (Texture.Texture is not { } texture) {
            return null;
        }

        CacheFields();

        var scale = Scale;
        var size = new Vector2(ClipRect!.Value.Width * scale.X, ClipRect.Value.Height * scale.Y);
        if (Rotation == 0f) {
            Vector2 pos = atPos - _multOrigin * scale + SubtextureOffset;

            return new Rectangle((int) pos.X, (int) pos.Y, (int) size.X, (int) size.Y);
        }

        // rotate our points, by rotating the offset
        var off = -_multOrigin;

        var p1 = off.Rotate(Rotation);
        var p2 = (off + new Vector2(size.X, 0)).Rotate(Rotation);
        var p3 = (off + new Vector2(0, size.Y)).Rotate(Rotation);
        var p4 = (off + size).Rotate(Rotation);

        var r1 = atPos + new Vector2(
            Math.Min(p4.X, Math.Min(p3.X, Math.Min(p1.X, p2.X))),
            Math.Min(p4.Y, Math.Min(p3.Y, Math.Min(p1.Y, p2.Y)))
        ) + SubtextureOffset.Rotate(Rotation);
        var r2 = atPos + new Vector2(
            Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
            Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y)))
        ) + SubtextureOffset.Rotate(Rotation);

        return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint());
    }

    private void CacheFields() {
        ClipRect ??= Texture.ClipRect;
        
        if (!_prepared) {
            _prepared = true;

            if (Width == 0)
                LoadSizeFromTexture();
            
            // sprites with dimensions not divible by 2 would get rendered at half pixel offsets while centering...
            var nonDivisibleBy2 = new Vector2(Width % 2, Height % 2);
            if (nonDivisibleBy2 != default)
                DrawOffset += (nonDivisibleBy2 * Origin);

            _multOrigin = (Origin * new Vector2(Width, Height)) + DrawOffset;
        }
    }
}

public record ColoredSpriteTemplate(SpriteTemplate Template, Color Color, Color OutlineColor) {
    public ColoredSpriteTemplate GetWithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha, 
        OutlineColor = OutlineColor * alpha,
    };
    
    public void RenderAt(Camera? cam, Vector2 offset, Vector2 pos)
        => Template.RenderAt(cam, offset, pos, Color, OutlineColor);
    
    public ColorTemplatedSprite Create(Vector2 pos)
        => new(this, pos);

    public ISprite CreateRecolored(Vector2 pos, Color color) => color == Color
        ? Create(pos)
        : Template.Create(pos, color);
}