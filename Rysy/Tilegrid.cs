using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy;

public class Tilegrid
{
    public int Width, Height;

    public char[,] Tiles = null!;

    public int? Depth { get; set; }

    public Autotiler? Autotiler;

    public CacheToken? CacheToken;

    public char SafeTileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return '0';

        return Tiles[x, y];
    }

    /// <summary>
    /// Safely sets a tile at (x,y). If this caused a change, returns true, false otherwise.
    /// </summary>
    public bool SafeSetTile(char tile, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return false;

        ref var currentTile = ref Tiles[x, y];
        if (currentTile == tile) {
            return false;
        }
        currentTile = tile;
        CacheToken?.Invalidate();
        return true;
    }

    public static unsafe Tilegrid FromString(int w, int h, string tilesString)
    {
        tilesString = tilesString.Replace("\r", "");
        w /= 8;
        h /= 8;

        var tiles = new char[w, h];
        tiles.Fill('0');

        var g = new Tilegrid()
        {
            Width = w,
            Height = h,
            Tiles = tiles,
        };

        int x = 0, y = 0;
        for (int ci = 0; ci < tilesString.Length; ci++)
        {
            var c = tilesString[ci];

            switch (c)
            {
            case '\n':
                while (x < w)
                {
                    tiles[x, y] = '0';
                    x++;
                }
                x = 0;
                y++;
                if (y >= h)
                {
                    return g;
                }
                break;
            default:
                if (x < w)
                {
                    tiles[x, y] = c is (char)0 or (char)13 ? '0' : c;
                    x++;
                }
                break;
            }
        }

        return g;
    }

    public IEnumerable<ISprite> GetSprites(Random random)
    {
        return Autotiler?.GetSprites(Vector2.Zero, Tiles, random).Select(s =>
        {
            s.Depth = Depth;
            return s;
        }) ?? throw new NullReferenceException("Tried to call GetSprites on a Tilegrid when Autotiler is null!");
    }
}
