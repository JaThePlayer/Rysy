using KeraLua;
using Rysy;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Selections;
using System.Text;

namespace Rysy.Graphics;

public class Tilegrid : ILuaWrapper {
    public Tilegrid() { }

    public Tilegrid(char[,] tiles) {
        Tiles = tiles;
    }

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
            CachedSprites = null;
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

    public char SafeTileAt(int x, int y, char def) {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return def;

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
        if (currentTile == tile)
            return false;
        currentTile = tile;
        RenderCacheToken?.Invalidate();
        if (CachedSprites is { } cached) {
            Autotiler!.UpdateSpriteList(cached, Tiles, x, y, true);
        }
        return true;
    }

    public void Resize(int widthPixels, int heightPixels) {
        Tiles = Tiles.CreateResized(widthPixels / 8, heightPixels / 8, '0');
        RenderCacheToken?.Invalidate();
        CachedSprites = null;
    }

    private Autotiler.AutotiledSpriteList? CachedSprites;

    public IEnumerable<ISprite> GetSprites() {
        if (CachedSprites is { } c)
            return c;

        var sprites = Autotiler?.GetSprites(Vector2.Zero, Tiles, Color.White).Select(s => {
            s.Depth = Depth;
            return s;
        }) ?? throw new NullReferenceException("Tried to call GetSprites on a Tilegrid when Autotiler is null!");

        CachedSprites = sprites.FirstOrDefault() as Autotiler.AutotiledSpriteList;

        return sprites;
    }

    public Selection? GetSelectionForArea(Rectangle area, SelectionLayer layer) {
        var handler = new TileSelectionHandler(this, area, layer);

        if (handler.AnyTileWithin())
            return new Selection() {
                Handler = handler,
            };

        return null;
    }

    #region Saving
    public static unsafe Tilegrid FromString(int widthPixels, int heightPixels, string tilesString) {
        var w = widthPixels / 8;
        var h = heightPixels / 8;

        var g = new Tilegrid(widthPixels, heightPixels);
        g.Tiles = TileArrayFromString(widthPixels, heightPixels, tilesString);

        return g;
    }

    public static unsafe char[,] TileArrayFromString(int widthPixels, int heightPixels, string tilesString) {
        tilesString = tilesString.Replace("\r", "", StringComparison.Ordinal);
        var w = widthPixels / 8;
        var h = heightPixels / 8;

        var tiles = new char[w,h];

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
                    if (y >= h)
                        return tiles;
                    break;
                default:
                    if (x < w) {
                        tiles[x, y] = c is (char) 0 or (char) 13 ? '0' : c;
                        x++;
                    }
                    break;
            }
        }

        return tiles;
    }

    public string GetSaveString() => GetSaveString(Tiles);

    public static string GetSaveString(char[,] tiles) {
        StringBuilder saveString = new();

        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);

        // stores 1 line of text + the newline character, to reduce heap allocations
        Span<char> line = stackalloc char[width + 1];
        for (int y = 0; y < height; y++) {
            line.Clear();
            for (int x = 0; x < width; x++) {
                var tile = tiles[x, y];
                line[x] = tile;
            }

            var endIdx = width;

            // Trim the line if it ends in air tiles
            while (endIdx > 0 && line[endIdx - 1] == '0')
                endIdx--;

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

    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "matrix":
                lua.PushWrapper(new MatrixLuaWrapper(this));
                return 1;
        }

        return 0;
    }

    private sealed record class MatrixLuaWrapper(Tilegrid Grid) : ILuaWrapper {
        private static int Get(nint n) {
            var lua = Lua.FromIntPtr(n);

            var wrapper = lua.UnboxWrapper<MatrixLuaWrapper>(1);
            var x = (int) lua.ToInteger(2);
            var y = (int) lua.ToInteger(3);
            var def = lua.FastToString(4);

            var tile = wrapper.Grid.SafeTileAt(x, y, def[0]);

            lua.PushString(tile.ToString());

            return 1;
        }

        public int LuaIndex(Lua lua, long key) {
            throw new NotImplementedException();
        }

        public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
            switch (key) {
                case "get":
                    lua.PushCFunction(Get);
                    return 1;
            }

            return 0;
        }
    }
}
