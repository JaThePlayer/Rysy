using Rysy.Extensions;
using Rysy.Selections;
using System.Collections;
using System.Collections.Generic;

namespace Rysy.Graphics;

/// <summary>
/// Interface which represents anything that can be rendered by Rysy's sprite rendering system.
/// </summary>
public interface ISprite : IEnumerable<ISprite> {
    public int? Depth { get; set; }

    public Color Color { get; set; }

    public ISprite WithMultipliedAlpha(float alpha);

    public bool IsLoaded { get; }

    public void Render(SpriteRenderCtx ctx);

    public ISelectionCollider GetCollider();

    public static ISpriteDepthComparer DepthDescendingComparer => new();

#pragma warning disable CA1033 // Interface methods should be callable by child types
    IEnumerator<ISprite> IEnumerable<ISprite>.GetEnumerator() => this.ToSelfEnumerator<ISprite>();

    IEnumerator IEnumerable.GetEnumerator() => this.ToSelfEnumerator();
#pragma warning restore CA1033 // Interface methods should be callable by child types

    public static Sprite FromTexture(string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Color = Color.White,
    };

    public static Sprite FromTexture(VirtTexture texture)
    => new(texture) {
        Color = Color.White,
    };

    public static Sprite FromTexture(Vector2 pos, string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Pos = pos.Floored(),
        Color = Color.White,
    };

    public static Sprite FromTexture(Vector2 pos, string texturePath, Vector2 origin)
    => new(GFX.Atlas[texturePath]) {
        Pos = pos.Floored(),
        Color = Color.White,
        Origin = origin,
    };

    public static Sprite FromTexture(Vector2 pos, VirtTexture texture)
    => new(texture) {
        Pos = pos.Floored(),
        Color = Color.White
    };
    
    public static SimpleSprite SimpleSpriteFromTexture(Vector2 pos, string texturePath)
        => new(GFX.Atlas[texturePath]) {
            Color = Color.White,
            Pos = pos,
        };

    public static Sprite FromSpriteBank(Vector2 pos, string name, string animation, SpriteBank? bank = null) {
        bank ??= EditorState.Map?.Sprites;

        if (bank is null) {
            return FromTexture(pos, GFX.VirtPixel);
        }

        var def = bank.Get(name);
        if (def is null) {
            Logger.Write("ISprite.FromSpriteBank", LogLevel.Warning, $"Missing SpriteBank entry: {name}");

            return FromTexture(pos, GFX.VirtPixel);
        }

        if (!def.Animations.TryGetValue(animation, out var anim)) {
            Logger.Write("ISprite.FromSpriteBank", LogLevel.Warning, $"Missing SpriteBank animation: {name}->{animation}");
            def.LogAsJson();

            return FromTexture(pos, GFX.VirtPixel);
        }

        return FromTexture(pos, anim.GetTexture(GFX.Atlas)) with {
            Origin = def.Origin
        };
    }

    public static NineSliceSprite NineSliceFromTexture(Vector2 pos, int w, int h, string texturePath) 
        => NineSliceFromTexture(new((int) pos.X, (int) pos.Y, w, h), texturePath);

    public static NineSliceSprite NineSliceFromTexture(Rectangle pos, string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Pos = pos,
        Color = Color.White,
    };

    public static RectangleSprite Point(Vector2 point, Color color)
    => new() {
        Pos = new((int)point.X, (int) point.Y, 1, 1),
        Color = color,
    };

    public static RectangleSprite Rect(Rectangle rect, Color color)
        => new() {
            Pos = rect,
            Color = color,
        };

    public static RectangleSprite Rect(Vector2 pos, int w, int h, Color color)
    => new() {
        Pos = new((int) pos.X, (int) pos.Y, w, h),
        Color = color,
    };

    public static RectangleSprite OutlinedRect(Rectangle rect, Color color, Color outlineColor, int outlineWidth = 1)
        => new() {
            Pos = rect,
            Color = color,
            OutlineColor = outlineColor,
            OutlineWidth = outlineWidth,
        };

    public static RectangleSprite OutlinedRect(Vector2 pos, int w, int h, Color color, Color outlineColor, int outlineWidth = 1)
    => new() {
        Pos = new((int) pos.X, (int) pos.Y, w, h),
        Color = color,
        OutlineColor = outlineColor,
        OutlineWidth = outlineWidth,
    };

    public static LineSprite Line(Vector2 start, Vector2 end, Color color)
     => new(new[] { start, end }) {
         Color = color,
     };

    public static LineSprite Line(IEnumerable<Vector2> positions, Color color)
     => new(positions) {
         Color = color
     };

    /// <summary>
    /// Calls <see cref="Line(Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework.Color)"/>, but floors the start and end positions.
    /// </summary>
    public static LineSprite LineFloored(Vector2 start, Vector2 end, Color color)
    => Line(start.Floored(), end.Floored(), color);

    /// <summary>
    /// Returns a sprite which renders a circle. The arguments mean the exact same thing as Draw.Circle in Monocle
    /// </summary>
    public static CircleSprite Circle(Vector2 center, float radius, Color color, int resolution, float thickness = 1f)
    => new() {
        Pos = center,
        Radius = radius,
        Color = color,
        Resulution = resolution,
        Thickness = thickness,
    };

    public static LineSprite GetCurveSprite(Vector2 start, Vector2 end, Vector2 middleOffset, Color color, int segments = 16)
    => new SimpleCurve {
        Start = start,
        End = end,
        Control = (start + end) / 2f + middleOffset
    }.GetSprite(color, segments);

    public static LinearGradientSprite LinearGradient(Rectangle bounds, string gradientString, LinearGradient.Directions dir, 
        bool loopX = false, bool loopY = false) {
        if (Graphics.LinearGradient.TryParse(gradientString, null, out var gradient)) {
            return new LinearGradientSprite(bounds, gradient, dir, loopX, loopY);
        }

        return new LinearGradientSprite(bounds, Graphics.LinearGradient.Parse("ff0000,ff0000,100", null), dir, loopX, loopY);
    }
    
    public static LinearGradientSprite LinearGradient(Rectangle bounds, LinearGradient gradient, LinearGradient.Directions dir, 
        bool loopX = false, bool loopY = false) {
        return new LinearGradientSprite(bounds, gradient, dir, loopX, loopY);
    }

    public static Rectangle GetBounds(IEnumerable<ISprite> sprites) {
        var rect = RectangleExt.Merge(sprites.Select(s => s.GetCollider().Rect));

        return rect;
    }
}

public static class ISpriteExtensions {
    public static IEnumerable<ISprite> SetDepth(this IEnumerable<ISprite> sprites, int depth) {
        foreach (var item in sprites) {
            item.Depth ??= depth;
            yield return item;
        }
    }
}