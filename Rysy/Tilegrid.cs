using Rysy.Graphics;
using Rysy.Helpers;
using System.Text;

namespace Rysy;

public class Tilegrid {
    public Tilegrid() { }

    public Tilegrid(int widthPixels, int heightPixels) {
        Tiles = new char[widthPixels / 8, heightPixels / 8];
        Tiles.Fill('0');
    }

    public int Width, Height;

    private char[,] _tiles = null!;
    public char[,] Tiles {
        get => _tiles;
        set {
            _tiles = value;
            Width = value.GetLength(0);
            Height = value.GetLength(1);
            RenderCacheToken?.Invalidate();
        }
    }

    public int? Depth { get; set; }

    private Autotiler? _autotiler;
    public Autotiler? Autotiler {
        get => _autotiler;
        set {
            _autotiler = value;

            // Make sure to clear the render cache whenever the autotiler data changes
            _autotiler!.TilesetDataCacheToken.OnInvalidate += () => {
                RenderCacheToken?.Invalidate();
            };

            RenderCacheToken?.Invalidate();
        }
    }

    /// <summary>
    /// A token which will be invalidated when the render cache needs to be cleared, for example when tiles are edited, or the Autotiler used is changed.
    /// The object that assigns this token is responsible for resetting the token once the render cache is reestablished.
    /// </summary>
    public CacheToken? RenderCacheToken;

    public char SafeTileAt(int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return '0';

        return Tiles[x, y];
    }

    /// <summary>
    /// Safely sets a tile at (x,y). If this caused a change, returns true, false otherwise.
    /// </summary>
    public bool SafeSetTile(char tile, int x, int y) {
        return SafeReplaceTile(tile, x, y, out _);
    }

    /// <summary>
    /// Safely sets a tile at (x,y). If this caused a change, returns true, false otherwise. Sets the previous tile at this location to <paramref name="oldTile"/>
    /// </summary>
    public bool SafeReplaceTile(char tile, int x, int y, out char oldTile) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            oldTile = '0';
            return false;
        }

        ref var currentTile = ref Tiles[x, y];
        oldTile = currentTile;
        if (currentTile == tile) {
            return false;
        }
        currentTile = tile;
        RenderCacheToken?.Invalidate();
        return true;
    }

    public void Resize(int widthPixels, int heightPixels) {
        Tiles = Tiles.CreateResized(widthPixels / 8, heightPixels / 8, '0');
        RenderCacheToken?.Invalidate();
    }

    public IEnumerable<ISprite> GetSprites() {
        return Autotiler?.GetSprites(Vector2.Zero, Tiles).Select(s => {
            s.Depth = Depth;
            return s;
        }) ?? throw new NullReferenceException("Tried to call GetSprites on a Tilegrid when Autotiler is null!");
    }

    #region Saving
    public static unsafe Tilegrid FromString(int widthPixels, int heightPixels, string tilesString) {
        tilesString = tilesString.Replace("\r", "");
        var w = widthPixels / 8;
        var h = heightPixels / 8;

        var g = new Tilegrid(widthPixels, heightPixels);
        var tiles = g.Tiles;

        int x = 0, y = 0;
        for (int ci = 0; ci < tilesString.Length; ci++) {
            var c = tilesString[ci];

            switch (c) {
                case '\n':
                    while (x < w) {
                        tiles[x, y] = '0';
                        x++;
                    }
                    x = 0;
                    y++;
                    if (y >= h) {
                        return g;
                    }
                    break;
                default:
                    if (x < w) {
                        tiles[x, y] = c is (char) 0 or (char) 13 ? '0' : c;
                        x++;
                    }
                    break;
            }
        }

        return g;
    }

    private string GetSaveString() {
        StringBuilder saveString = new();

        // stores 1 line of text + the newline character, to reduce heap allocations
        Span<char> line = stackalloc char[Width + 1];
        for (int y = 0; y < Height; y++) {
            line.Clear();
            for (int x = 0; x < Width; x++) {
                var tile = Tiles[x, y];
                line[x] = tile;
            }

            var endIdx = Width;

            // Trim the line if it ends in air tiles
            while (endIdx > 0 && line[endIdx - 1] == '0') {
                endIdx--;
            }

            line[endIdx] = '\n';
            var slice = line[0..(endIdx + 1)];
            saveString.Append(slice);
        }

        return saveString.ToString();
    }

    public BinaryPacker.Element Pack(string name) {
        var saveString = GetSaveString();

        return new(name) {
            Attributes = new() {
                ["innerText"] = saveString,
            }
        };
    }

    #endregion
}
