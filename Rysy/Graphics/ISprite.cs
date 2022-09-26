namespace Rysy.Graphics;

public interface ISprite
{
    public int? Depth { get; set; }

    public Color Color { get; set; }

    public float Alpha { get; set; }

    public bool IsLoaded { get; }

    public void Render();

    public static ISpriteDepthComparer DepthDescendingComparer = new();

    public static Sprite FromTexture(Vector2 pos, string texturePath)
    => new(GFX.Atlas[texturePath])
    {
        Pos = pos,
        Color = Color.White,
    };

    public static Sprite FromTexture(Vector2 pos, VirtTexture texture)
    => new(texture)
    {
        Pos = pos,
        Color = Color.White
    };

    public static RectangleSprite Rect(Rectangle rect, Color color)
        => new()
        {
            Pos = rect,
            Color = color,
        };

    public static RectangleSprite Rect(Vector2 pos, int w, int h, Color color)
    => new()
    {
        Pos = new((int)pos.X, (int)pos.Y, w, h),
        Color = color,
    };

    public static RectangleSprite HollowRect(Rectangle rect, Color color, Color outlineColor)
        => new()
        {
            Pos = rect,
            Color = color,
            OutlineColor = outlineColor,
        };

    public static RectangleSprite HollowRect(Vector2 pos, int w, int h, Color color, Color outlineColor)
    => new()
    {
        Pos = new((int)pos.X, (int)pos.Y, w, h),
        Color = color,
        OutlineColor = outlineColor,
    };

    public static LineSprite Line(Vector2 start, Vector2 end, Color color)
    {
        return new LineSprite(new[] { start, end })
        {
            Color = color,
        };
    }

    public static IEnumerable<ISprite> GetNineSliceSprites(Sprite baseSprite, Vector2 pos, int w, int h, int tileSize)
    {
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                yield return baseSprite.CreateSubtexture(x switch
                {
                    0 => 0,
                    _ when x == w - 1 => tileSize * 2,
                    _ => tileSize
                }, y switch
                {
                    0 => 0,
                    _ when y == h - 1 => tileSize * 2,
                    _ => tileSize
                }, tileSize, tileSize) with
                {
                    Pos = pos + new Vector2(x * 8, y * 8),
                    Origin = new(),
                };
            }
        }
    }

    public static IEnumerable<ISprite> Curve(Vector2 start, Vector2 end, Vector2 middleOffset, Color color, int segments = 16)
    => new SimpleCurve
    {
        Start = start,
        End = end,
        Control = (start + end) / 2f + middleOffset
    }.GetSprites(color, segments);
}

public static class ISpriteExtensions
{
    public static IEnumerable<ISprite> SetDepth(this IEnumerable<ISprite> sprites, int depth)
    {
        foreach (var item in sprites)
        {
            item.Depth ??= depth;
            yield return item;
        }
    }
}