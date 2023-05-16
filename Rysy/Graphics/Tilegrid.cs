using KeraLua;
using Rysy;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using Rysy.LuaSupport;
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
        return true;
    }

    public void Resize(int widthPixels, int heightPixels) {
        Tiles = Tiles.CreateResized(widthPixels / 8, heightPixels / 8, '0');
        RenderCacheToken?.Invalidate();
    }

    public IEnumerable<ISprite> GetSprites() {
        return Autotiler?.GetSprites(Vector2.Zero, Tiles, Color.White).Select(s => {
            s.Depth = Depth;
            return s;
        }) ?? throw new NullReferenceException("Tried to call GetSprites on a Tilegrid when Autotiler is null!");
    }

    public Selection? GetSelectionForArea(Rectangle area, SelectionLayer layer) {
        var handler = new RectSelectionHandler(this, area, layer);

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
                    if (y >= h)
                        return g;
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

    public int Lua__index(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "matrix":
                lua.PushWrapper(new MatrixLuaWrapper(this));
                return 1;
        }

        return 0;
    }

    internal class RectSelectionHandler : ISelectionHandler, ISelectionCollider, ISelectionFlipHandler {
        public Tilegrid Grid;
        public Rectangle Rect;

        private List<(bool Exclude, Rectangle)> ToMoveRects = new();

        private char[,] ToMove;
        private char[,]? Orig;

        private Vector2 RemainderOffset;

        private SelectionLayer SelectionLayer;

        public RectSelectionHandler(Tilegrid grid, Rectangle rectPixels, SelectionLayer layer) {
            (Grid, Rect) = (grid, rectPixels);
            ToMoveRects.Add((false, Rect));
            SelectionLayer = layer;
        }

        internal RectSelectionHandler(Tilegrid grid, Rectangle rectPixels, char[,] toMove, SelectionLayer layer) {
            (Grid, Rect) = (grid, rectPixels);
            ToMoveRects.Add((false, Rect));

            ToMove = toMove;
            Orig = (char[,]) Grid.Tiles.Clone();
            SelectionLayer = layer;
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

        private void HandleToMove(char[,] toMove, Rectangle rect, bool exclude, int offX, int offY) {
            for (int x = rect.X; x < rect.Right; x++)
                for (int y = rect.Y; y < rect.Bottom; y++)
                    toMove[x - offX, y - offY] = exclude ? '0' : Grid.SafeTileAt(x, y);
        }

        public IHistoryAction MoveBy(Vector2 offset) {
            var tileOffset = ((offset + RemainderOffset) / 8).ToPoint();

            // since offset might be less than 8, let's accumulate the offsets that weren't sufficient to move tiles.
            RemainderOffset += offset - tileOffset.ToVector2() * 8;

            if (tileOffset.X == 0 && tileOffset.Y == 0)
                return new MergedAction(Array.Empty<IHistoryAction>());

            ConsumeTilesIfNeeded();

            var action = new TileRectMoveAction(Grid, Rect.Div(8), Orig, ToMove, tileOffset);
            var moveOffset = tileOffset.ToVector2() * 8;

            Rect = Rect.MovedBy(moveOffset);
            for (int i = 0; i < ToMoveRects.Count; i++)
                ToMoveRects[i] = (ToMoveRects[i].Exclude, ToMoveRects[i].Item2.MovedBy(moveOffset));
            return action;
        }

        private void ConsumeTilesIfNeeded() {
            if (Orig is null) {
                Orig ??= (char[,]) Grid.Tiles.Clone();
                var rect = Rect.Div(8);

                var toMove = ToMove ??= CreateToMove();

                for (int x = rect.X; x < rect.Right; x++)
                    for (int y = rect.Y; y < rect.Bottom; y++)
                        if (x >= 0 && y >= 0 && x < Grid.Width && y < Grid.Height
                            && toMove[x - rect.X, y - rect.Y] != '0')
                            Orig[x, y] = '0';
            }
        }

        private char[,] CreateToMove() {
            var rect = Rect.Div(8);
            var (w, h) = (rect.Width, rect.Height);

            var toMove = new char[w, h];
            toMove.Fill('0');
            foreach (var (exclude, r) in ToMoveRects)
                HandleToMove(toMove, r.Div(8), exclude, rect.X, rect.Y);

            return toMove;
        }

        // collider:
        public bool IsWithinRectangle(Rectangle roomPos) {
            if (!Rect.Intersects(roomPos))
                return false;

            var rect = roomPos.Div(8);
            rect.Width = rect.Width.AtLeast(1);
            rect.Height = rect.Height.AtLeast(1);

            if (ToMove is not { } toMove)
                toMove = CreateToMove();
            var offX = Rect.Div(8).X;
            var offY = Rect.Div(8).Y;
            for (int x = rect.X; x < rect.Right; x++)
                for (int y = rect.Y; y < rect.Bottom; y++)
                    if (toMove.GetOrDefault(x - offX, y - offY, '0') != '0')
                        return true;

            return false;
        }

        public void Render(Color c) {
            foreach (var item in GetSprites(c))
                item.Render();
        }

        internal IEnumerable<ISprite> GetSprites(Color c, Vector2? pos = null) {
            var rect = Rect.Div(8);

            Vector2 rPos = pos ?? new(rect.X * 8, rect.Y * 8);

            ToMove ??= CreateToMove();

            if (ToMove is { } toMove)                 
                // if we've moved the tiles, make sure to render ToMove instead of the current grid,
                // as otherwise we might render an outline on a tile that's within range, but not actually selected
                for (int x = 0; x < toMove.GetLength(0); x++)
                    for (int y = 0; y < toMove.GetLength(1); y++)
                        if (toMove[x, y] != '0')
                            yield return ISprite.OutlinedRect(new(x * 8 + rPos.X, y * 8 + rPos.Y), 8, 8, c * 0.3f, c * 0.7f);
        }

        public void MergeWith(Rectangle rectPixels, bool exclude) {
            rectPixels = rectPixels.Div(8).AddSize(1, 1).Mult(8);

            var merged = RectangleExt.Merge(Rect, rectPixels);
            Rect = merged;
            ToMoveRects.Add((exclude, rectPixels));

            if (ToMove is { }) {
                ToMove = null!;
                Orig = null;
            }
        }

        public IHistoryAction? TryResize(Point delta) {
            return null;
        }

        public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
            return null;
        }

        public BinaryPacker.Element? PackParent() {
            var toMove = ToMove ?? CreateToMove();

            return new("tiles") {
                Attributes = new() {
                    ["text"] = GetSaveString(toMove),
                    ["w"] = toMove.GetLength(0),
                    ["h"] = toMove.GetLength(1),
                    ["x"] = Rect.X,
                    ["y"] = Rect.Y,
                }
            };
        }

        public void RenderSelection(Color c) => Render(c);

        public void ClearCollideCache() {

        }

        public void OnRightClicked(IEnumerable<Selection> selections) {
        }

        public IHistoryAction PlaceClone(Room room) {
            Console.WriteLine(Rect.Div(8).Location);
            return new TilePasteAction(new(ToMove), Grid, Rect.Div(8).Location);
        }

        public IHistoryAction PlaceCloneAt(Room room, Point tilePos) {
            return new TilePasteAction(new(ToMove), Grid, tilePos);
        }

        public IHistoryAction? TryFlipHorizontal() {
            ConsumeTilesIfNeeded();

            var newToMove = ToMove.CreateFlippedHorizontally();

            var action = new TileSwapAction(Grid, Rect.Div(8), Orig!, newToMove);
            ToMove = newToMove;

            return action;
        }

        public IHistoryAction? TryFlipVertical() {
            ConsumeTilesIfNeeded();

            var newToMove = ToMove.CreateFlippedVertically();

            var action = new TileSwapAction(Grid, Rect.Div(8), Orig!, newToMove);
            ToMove = newToMove;

            return action;
        }

        public SelectionLayer Layer => SelectionLayer;
    }

    private record class MatrixLuaWrapper(Tilegrid Grid) : ILuaWrapper {
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

        public int Lua__index(Lua lua, long key) {
            throw new NotImplementedException();
        }

        public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
            switch (key) {
                case "get":
                    lua.PushCFunction(Get);
                    return 1;
            }

            return 0;
        }
    }
}
