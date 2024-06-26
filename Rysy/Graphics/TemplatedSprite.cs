﻿using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Graphics;

/// <summary>
/// A sprite using <see cref="SpriteTemplate"/>, with a field for changing Color. Always has no outline, use <see cref="TemplatedOutlinedSprite"/> if needed.
/// </summary>
public record struct TemplatedSprite(SpriteTemplate Template) : ITextureSprite {
    public TemplatedSprite(SpriteTemplate template, Vector2 pos, Color color) : this(template) {
        Pos = pos;
        Color = color;
    }
    
    public int? Depth {
        get => Template.Depth;
        set {
            // noop
        }
    }
    
    public Vector2 Pos { get; set; }
    public Color Color { get; set; }
    
    public ISprite WithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha,
    };

    public bool IsLoaded => Template.IsLoaded;
    
    public void Render(SpriteRenderCtx ctx) {
        Template.RenderAt(ctx, Pos, Color, default);
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);

    public Rectangle? GetRenderRect() => Template.GetRenderRect(Pos);
}

/// <summary>
/// A sprite using <see cref="SpriteTemplate"/>, using animated rainbow colors.
/// Always has no outline, use <see cref="TemplatedOutlinedSprite"/> if needed.
/// </summary>
public record struct TemplatedRainbowSprite(SpriteTemplate Template) : ITextureSprite {
    public TemplatedRainbowSprite(SpriteTemplate template, Vector2 pos) : this(template) {
        Pos = pos;
    }
    
    public int? Depth {
        get => Template.Depth;
        set {
            // noop
        }
    }
    
    public Vector2 Pos { get; set; }
    
    private float Alpha = 1f;

    [Obsolete("Only used to implement ISprite, unused otherwise.")]
    public Color Color {
        get => ColorHelper.GetRainbowColor(Room.DummyRoom, Pos);
        set => Alpha = value.A / 255f;
    }
    
    public ISprite WithMultipliedAlpha(float alpha) => this with {
        Alpha = Alpha * alpha,
    };

    public bool IsLoaded => Template.IsLoaded;

    public void Render(SpriteRenderCtx ctx) {
        var pos = Pos;
        var room = ctx.Room ?? Room.DummyRoom;
        var color = ctx.Animate
            ? ColorHelper.GetRainbowColorAnimated(room, pos)
            : ColorHelper.GetRainbowColor(room, pos);
        
        Template.RenderAt(ctx, pos, color * Alpha, default);
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);

    public Rectangle? GetRenderRect() => Template.GetRenderRect(Pos);
}

/// <summary>
/// A sprite using <see cref="SpriteTemplate"/>, with a field for changing Color and OutlineColor.
/// </summary>
public record struct TemplatedOutlinedSprite(SpriteTemplate Template) : ITextureSprite {
    public TemplatedOutlinedSprite(SpriteTemplate template, Vector2 pos, Color color, Color outlineColor) : this(template) {
        Pos = pos;
        Color = color;
        OutlineColor = outlineColor;
    }
    
    public int? Depth {
        get => Template.Depth;
        set {
            // noop
        }
    }
    
    public Vector2 Pos { get; set; }
    public Color Color { get; set; }
    public Color OutlineColor { get; set; }
    
    public ISprite WithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha,
        OutlineColor = OutlineColor * alpha
    };

    public bool IsLoaded => Template.IsLoaded;

    public void Render(SpriteRenderCtx ctx) {
        Template.RenderAt(ctx, Pos, Color, OutlineColor);
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);

    public Rectangle? GetRenderRect() => Template.GetRenderRect(Pos);
}

/// <summary>
/// A sprite using a <see cref="ColoredSpriteTemplate"/>. Most memory-efficient sprite type.
/// </summary>
public record struct ColorTemplatedSprite : ITextureSprite {
    public ColorTemplatedSprite(ColoredSpriteTemplate template, Vector2 pos) {
        Template = template;
        Pos = pos;
    }
    
    public ColoredSpriteTemplate Template { get; private set; }
    public Vector2 Pos { get; set; }
    
    public int? Depth {
        get => Template.Template.Depth;
        set {
            // noop
        }
    }
    
    public Color Color {
        get => Template.Color;
        set => Template = Template with { Color = value };
    }
    
    public Color OutlineColor => Template.OutlineColor;
    
    public ISprite WithMultipliedAlpha(float alpha) => OutlineColor == default 
        ? new TemplatedSprite(Template.Template, Pos, Color * alpha)
        : new TemplatedOutlinedSprite(Template.Template, Pos, Color * alpha, OutlineColor * alpha);

    public bool IsLoaded => Template.Template.IsLoaded;

    public void Render(SpriteRenderCtx ctx) {
        Template.RenderAt(ctx, Pos);
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);

    public Rectangle? GetRenderRect() => Template.Template.GetRenderRect(Pos);
}