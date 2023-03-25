using Rysy.Extensions;

namespace Rysy.Graphics;

/// <summary>
/// Interface which represents anything that can be rendered by Rysy's sprite rendering system.
/// </summary>
public interface ISprite {
    public int? Depth { get; set; }

    public Color Color { get; set; }

    public void MultiplyAlphaBy(float alpha);

    public bool IsLoaded { get; }

    public void Render();
    public void Render(Camera? cam, Vector2 offset);

    public static ISpriteDepthComparer DepthDescendingComparer = new();

    public static Sprite FromTexture(string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Color = Color.White,
    };

    public static Sprite FromTexture(Vector2 pos, string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Pos = pos.Floored(),
        Color = Color.White,
    };

    public static Sprite FromTexture(Vector2 pos, VirtTexture texture)
    => new(texture) {
        Pos = pos.Floored(),
        Color = Color.White
    };

    public static NineSliceSprite NineSliceFromTexture(Vector2 pos, int w, int h, string texturePath) 
        => NineSliceFromTexture(new((int) pos.X, (int) pos.Y, w, h), texturePath);

    public static NineSliceSprite NineSliceFromTexture(Rectangle pos, string texturePath)
    => new(GFX.Atlas[texturePath]) {
        Pos = pos,
        Color = Color.White,
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

    /// <summary>
    /// Calls <see cref="Line(Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework.Color)"/>, but floors the start and end positions.
    /// </summary>
    public static LineSprite LineFloored(Vector2 start, Vector2 end, Color color)
    => Line(start.Floored(), end.Floored(), color);

    /// <summary>
    /// Returns a sprite which renders a circle. The arguments mean the exact same thing as Draw.Circle in Monocle
    /// </summary>
    public static CircleSprite Circle(Vector2 center, float radius, Color color, int resolution)
    => new() {
        Pos = center,
        Radius = radius,
        Color = color,
        Resulution = resolution,
    };

    public static IEnumerable<ISprite> GetCurveSprites(Vector2 start, Vector2 end, Vector2 middleOffset, Color color, int segments = 16)
    => new SimpleCurve {
        Start = start,
        End = end,
        Control = (start + end) / 2f + middleOffset
    }.GetSprites(color, segments);
}

public static class ISpriteExtensions {
    public static IEnumerable<ISprite> SetDepth(this IEnumerable<ISprite> sprites, int depth) {
        foreach (var item in sprites) {
            item.Depth ??= depth;
            yield return item;
        }
    }
}