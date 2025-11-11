using Rysy.Extensions;
using System.Runtime.CompilerServices;

namespace Rysy.Graphics;

/// <summary>
/// Represents a template that can be used to construct <see cref="TemplatedSprite"/> or <see cref="TemplatedOutlinedSprite"/> instances.
/// Used to reduce memory footprint of sprites and improve performance.
/// </summary>
public sealed record SpriteTemplate {
    public static SpriteTemplate FromTexture(string path, int depth)
        => FromTexture(Gfx.Atlas[path], depth);
    
    public static SpriteTemplate FromTexture(VirtTexture texture, int depth) {
        return new() {
            Texture = texture,
            _drawOffset = texture.DrawOffset,
            Depth = depth,
        };
    }

    public static SpriteTemplate FromSprite(Sprite sprite) {
        return new() {
            Texture = sprite.Texture, 
            _drawOffset = sprite.DrawOffset,
            Depth = sprite.Depth ?? 0,
            Origin = sprite.Origin,
            Scale = sprite.Scale,
            Rotation = sprite.Rotation,
        };
    }

    public SpriteTemplate WithTexture(VirtTexture newTex) => this with {
        Texture = newTex,
        _drawOffset = newTex.DrawOffset
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
    
    public TemplatedRainbowSprite CreateRainbow(Vector2 pos)
        => new(this, pos);

    public RepeatingSprite CreateRepeating(Rectangle bounds, Color color)
        => new(this, bounds, color);

    public Sprite CreateUntemplated(Vector2 pos, Color color) => new Sprite(Texture) {
        Color = color,
        Pos = pos,
        Depth = Depth,
        Origin = Origin,
        Rotation = Rotation,
        Scale = Scale,
    };

    public ColoredSpriteTemplate CreateColoredTemplate(Color color) =>
        new(this, color, default);
    
    public ColoredSpriteTemplate CreateColoredTemplate(Color color, Color outlineColor) =>
        new(this, color, outlineColor);
    
    public int Depth { get; init; }
    public VirtTexture Texture { get; private init; }
    public Rectangle? ClipRect { get; internal set; }
    
    private Vector2 _originBacking;
    public Vector2 Origin {
        get => _originBacking;
        set {
            _originBacking = value;
            MarkChanged();
        }
    }
    
    public float Rotation { get; set; } = 0f;
    
    private Vector2 _scaleBacking = Vector2.One;
    public Vector2 Scale {
        get => _scaleBacking;
        set {
            _scaleBacking = value;
            MarkChanged();
        }
    }

    public bool IsLoaded => Texture.Texture is { };
    
    private Vector2 _drawOffset;
    private int _width;
    private int _height;
    private Vector2 _subtextureOffset;
    
    // Origin multiplied by the size of the texture, needed to pass it to SpriteBatch
    private Vector2 _multOrigin;
    // The absolute value of Scale, simplifies logic and needed to pass it to monogame's SpriteBatch
    private Vector2 _realScale;
    
    private bool _prepared;
    internal SpriteEffects Flip;

    public void RenderAt(SpriteRenderCtx ctx, Vector2 pos, Color color, Color outlineColor) {
        if (Texture.Texture is not { } texture)
            return;
        
        RenderAt(ctx, pos, color, outlineColor, texture);
    }
    
    public void RenderAt(SpriteRenderCtx ctx, Vector2 pos, Color color, Color outlineColor, Texture2D? texture) {
        if (texture is null)
            return;
        
        // store some fields for later use
        // this is not done in the constructor, as that would force preloading
        if (!_prepared)
            CacheFields();

        var rotation = Rotation;
        var origin = _multOrigin;
        var scale = _realScale;

        if (rotation == 0f) {
            // todo: figure out if calculating rotated rectangles for culling is worth it
            if (ctx.Camera is { } cam) {
                var rPos = pos - origin * scale;
                if (!cam.IsRectVisible(rPos + ctx.CameraOffset, (int)(_width * scale.X), (int) (_height * scale.Y)))
                    return;
            }

            pos += _subtextureOffset;
        } else {
            pos += _subtextureOffset.Rotate(rotation);
        }

        var flip = Flip;
        var batch = Gfx.Batch;

        if (outlineColor != default) {
            batch.Draw(texture, pos + new Vector2(-1f, 0f), ClipRect, outlineColor, rotation, origin, scale, flip, 0f);
            batch.Draw(texture, pos + new Vector2(1f, 0f), ClipRect, outlineColor, rotation, origin, scale, flip, 0f);
            batch.Draw(texture, pos + new Vector2(0f, 1f), ClipRect, outlineColor, rotation, origin, scale, flip, 0f);
            batch.Draw(texture, pos + new Vector2(0f, -1f), ClipRect, outlineColor, rotation, origin, scale, flip, 0f);
        }

        if (color != default)
            batch.Draw(texture, pos, ClipRect, color, rotation, origin, scale, flip, 0f);
    }
    
    private void LoadSizeFromTexture() {
        _width = Texture.Width;
        _height = Texture.Height;
    }
    
    public Rectangle? GetRenderRect(Vector2 atPos) {
        if (Texture.Texture is not { } texture) {
            return null;
        }
        
        if (!_prepared)
            CacheFields();

        var scale = _realScale;
        var size = new Vector2(ClipRect!.Value.Width * scale.X, ClipRect.Value.Height * scale.Y);
        if (Rotation == 0f) {
            Vector2 pos = atPos - _multOrigin * scale + _subtextureOffset;

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
        ) + _subtextureOffset.Rotate(Rotation);
        var r2 = atPos + new Vector2(
            Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
            Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y)))
        ) + _subtextureOffset.Rotate(Rotation);

        return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint());
    }

    private void CacheFields() {
        if (_prepared)
            return;
        
        _prepared = true;
            
        ClipRect ??= Texture.ClipRect;

        if (_width == 0)
            LoadSizeFromTexture();
            
        // sprites with dimensions not divible by 2 would get rendered at half pixel offsets while centering...
        var nonDivisibleBy2 = new Vector2(_width % 2, _height % 2);
        if (nonDivisibleBy2 != default)
            _drawOffset += (nonDivisibleBy2 * Origin);

        _multOrigin = (Origin * new Vector2(_width, _height)) + _drawOffset;
        _realScale = Scale;
            
        if (Scale.X < 0) {
            _realScale.X = -Scale.X;
            Flip ^= SpriteEffects.FlipHorizontally;
            _multOrigin.X = ClipRect!.Value.Width - _multOrigin.X;
        }
        if (Scale.Y < 0) {
            _realScale.Y = -Scale.Y;
            Flip ^= SpriteEffects.FlipVertically;
            _multOrigin.Y = ClipRect!.Value.Height - _multOrigin.Y;
        }
    }

    private void MarkChanged() {
        _prepared = false;
        _drawOffset = Texture.DrawOffset;
        Flip = SpriteEffects.None;
    }
}

public sealed record ColoredSpriteTemplate {
    public SpriteTemplate Template { get; init; }
    public Color Color { get; set; }
    public Color OutlineColor { get; set; }
    
    public ColoredSpriteTemplate(SpriteTemplate template, Color color, Color outlineColor) {
        Template = template;
        Color = color;
        OutlineColor = outlineColor;
    }
    
    public ColoredSpriteTemplate GetWithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha, 
        OutlineColor = OutlineColor * alpha,
    };
    
    public void RenderAt(SpriteRenderCtx ctx, Vector2 pos)
        => Template.RenderAt(ctx, pos, Color, OutlineColor);
    
    public ColorTemplatedSprite Create(Vector2 pos)
        => new(this, pos);
    
    public TemplatedRainbowSprite CreateRainbow(Vector2 pos)
        => new(Template, pos);

    public ISprite CreateRecolored(Vector2 pos, Color color) => color == Color
        ? Create(pos)
        : Template.Create(pos, color);
}

public sealed record AnimatedSpriteTemplate(SpriteTemplate Template, ITextureSource TextureSource) {
    private List<SpriteTemplate> _realTemplates;
    
    public void RenderAt(SpriteRenderCtx ctx, Vector2 pos, Color color, Color outlineColor, float timeOffset = 0f) {
        if (_realTemplates is not {} realTemplates) {
            _realTemplates = new(TextureSource.TextureCount);
            realTemplates = _realTemplates;
            for (int j = 0; j < TextureSource.TextureCount; j++) {
                realTemplates.Add(Template.WithTexture(TextureSource.GetTextureByIndex(j)));
            }
        }
        
        var textureIdx = TextureSource.GetTextureIndex(ctx.Time + timeOffset);
        var animatedTemplate = textureIdx >= 0 && textureIdx < realTemplates.Count ? realTemplates[textureIdx] : default;
        
        if (animatedTemplate is { Texture.Texture: {} templateTexture }) {
            animatedTemplate.RenderAt(ctx, pos, color, outlineColor, templateTexture);
        } else {
            Template.RenderAt(ctx, pos, color, outlineColor);
        }
    }

    public TemplatedAnimatedSprite Create(Vector2 pos, Color color) => new(this) { Pos = pos, Color = color };
}

public record SimpleAnimation(IReadOnlyList<VirtTexture> Textures, float AnimationSpeed) : ITextureSource {
    public int TextureCount => Textures.Count;
    
    public int GetTextureIndex(float time) {
        if (Textures.Count < 1) {
            return -1;
        }
        
        var i = (int)(time * AnimationSpeed) % Textures.Count;
        
        return i.AtLeast(0);
    }

    public VirtTexture GetTextureByIndex(int index) => Textures[index];

    public static SimpleAnimation FromPathSubtextures(string path, float animationSpeed) {
        var textures = Gfx.Atlas.GetSubtextures(path);

        return new(textures, animationSpeed);
    }
}

public interface ITextureSource {
    public int TextureCount { get; }
    
    public int GetTextureIndex(float time);

    public VirtTexture GetTextureByIndex(int index);
}