﻿using Rysy.Extensions;
using Rysy.Selections;

namespace Rysy.Graphics;

/// <summary>
/// Allows rendering PICO-8 text, centered into a rectangle.
/// </summary>
public record struct PicoTextRectSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    public bool IsLoaded => true;

    public string Text;
    public Rectangle Pos;
    public float Scale = 1f;

    public PicoTextRectSprite(string text) {
        Text = text;
    }

    public void Render() {
        PicoFont.Print(Text, Pos, Color, Scale);
    }

    public void Render(SpriteRenderCtx ctx) {
        if (ctx.Camera is { } cam) {
            if (!cam.IsRectVisible(Pos.MovedBy(ctx.CameraOffset)))
                return;
        }

        Render();
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromRect(Pos);
}
