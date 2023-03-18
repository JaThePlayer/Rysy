using KeraLua;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.LuaSupport;
using System.Text;

namespace Rysy;

public class Tilegrid : ILuaWrapper {
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

    public Selection? GetSelectionForArea(Rectangle area) {
        var handler = new RectSelectionHandler(this, area);

        if (handler.AnyTileWithin())
            return new Selection() { 
                Handler = handler,
            };

        return null;
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

    public int Lua__index(Lua lua, object key) {
        switch (key) {
            case "matrix":
                lua.PushWrapper(new MatrixLuaWrapper(this));
                return 1;
        }

        return 0;
    }

    internal class RectSelectionHandler : ISelectionHandler, ISelectionCollider {
        public Tilegrid Grid;
        public Rectangle Rect;

        private char[,] ToMove;
        private char[,]? Orig;

        private Vector2 RemainderOffset;

        public RectSelectionHandler(Tilegrid grid, Rectangle rectPixels) {
            (Grid, Rect) = (grid, rectPixels);
        }

        public bool AnyTileWithin() {
            var rect = Rect.Div(8);
            for (int x = rect.X; x < rect.Right; x++)
                for (int y = rect.Y; y < rect.Bottom; y++)
                    if (Grid.SafeTileAt(x, y) != '0')
                        return true;

            return false;
        }

        public object Parent => Grid;

        public IHistoryAction DeleteSelf() {
            return new TileRectChangeAction('0', Rect.Div(8), Grid);
        }

        public IHistoryAction MoveBy(Vector2 offset) {
            var tileOffset = ((offset + RemainderOffset) / 8).ToPoint();

            // since offset might be less than 8, let's accumulate the offsets that weren't sufficient to move tiles.
            RemainderOffset += offset - (tileOffset.ToVector2() * 8);

            if (tileOffset.X == 0 && tileOffset.Y == 0) {
                return new MergedAction(Array.Empty<IHistoryAction>());
            }

            if (Orig is null) {
                Orig ??= (char[,])Grid.Tiles.Clone();
                var rect = Rect.Div(8);
                var (w, h) = (rect.Width, rect.Height);

                var toMove = new char[w, h];
                for (int x = rect.X; x < rect.Right; x++)
                    for (int y = rect.Y; y < rect.Bottom; y++)
                        toMove[x - rect.X, y - rect.Y] = Grid.SafeTileAt(x, y);
                ToMove = toMove;

                for (int x = rect.X; x < rect.Right; x++)
                    for (int y = rect.Y; y < rect.Bottom; y++) {
                        if (x >= 0 && y >= 0 && x < Grid.Width && y < Grid.Height)
                            Orig[x, y] = '0';
                    }
            }

            var action = new TileRectMoveAction(Grid, Rect.Div(8), Orig, ToMove, tileOffset);
            Rect = Rect.MovedBy(tileOffset.ToVector2() * 8);
            return action;
        }

        // collider:

        public bool IsWithinRectangle(Rectangle roomPos) {
            if (!Rect.Intersects(roomPos))
                return false;

            var rect = roomPos.Div(8);
            rect.Width = rect.Width.AtLeast(1);
            rect.Height = rect.Height.AtLeast(1);

            if (ToMove is { } toMove) {
                // if we've moved the tiles, make sure to render ToMove instead of the current grid,
                // as otherwise we might render an outline on a tile that's within range, but not actually selected
                for (int x = 0; x < ToMove.GetLength(0); x++)
                    for (int y = 0; y < ToMove.GetLength(1); y++) {
                        if (ToMove[x, y] != '0') {
                            return true;
                        }
                    }
            } else {
                for (int x = rect.X; x < rect.Right; x++)
                    for (int y = rect.Y; y < rect.Bottom; y++) {
                        if (Grid.SafeTileAt(x, y) != '0') {
                            return true;
                        }
                    }
            }

            return false;
        }

        public void Render(Color c) {
            var rect = Rect.Div(8);

            if (ToMove is { } toMove) {
                // if we've moved the tiles, make sure to render ToMove instead of the current grid,
                // as otherwise we might render an outline on a tile that's within range, but not actually selected
                for (int x = 0; x < ToMove.GetLength(0); x++)
                    for (int y = 0; y < ToMove.GetLength(1); y++) {
                        if (ToMove[x, y] != '0') {
                            ISprite.OutlinedRect(new((x + rect.X) * 8, (y + rect.Y) * 8), 8, 8, c * 0.3f, c * 0.7f).Render();
                        }
                    }
            } else {
                for (int x = rect.X; x < rect.Right; x++)
                    for (int y = rect.Y; y < rect.Bottom; y++) {
                        if (Grid.SafeTileAt(x, y) != '0') {
                            ISprite.OutlinedRect(new((x) * 8, (y) * 8), 8, 8, c * 0.3f, c * 0.7f).Render();
                        }
                    }
            }

        }

        public IHistoryAction? TryResize(Point delta) {
            return null;
        }

        public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
            return null;
        }

        public BinaryPacker.Element? PackParent() {
            return null;
        }

        public void RenderSelection(Color c) => Render(c);

        public void ClearCollideCache() {
            
        }

        public void OnRightClicked(IEnumerable<Selection> selections) {
        }

        public SelectionLayer Layer => SelectionLayer.None;
    }

    private record class MatrixLuaWrapper(Tilegrid Grid) : ILuaWrapper {
        public int Lua__index(Lua lua, object key) {
            switch (key) {
                case "get":
                    lua.PushCFunction(Get);
                    return 1;
            }

            return 0;
        }

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
    }
}
