using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Selections;
public sealed class TileSelectionHandler : ISelectionHandler, ISelectionCollider, ISelectionFlipHandler {
    public Tilegrid Grid;
    public Rectangle Rect;

    private List<(bool Exclude, Rectangle)> _toMoveRects = new();

    private char[,] _toMove;
    private char[,]? _orig;

    private SelectionLayer _selectionLayer;

    public bool ResizableX => false;

    public bool ResizableY => false;

    public TileSelectionHandler(Tilegrid grid, Rectangle rectPixels, SelectionLayer layer) {
        (Grid, Rect) = (grid, rectPixels);
        _toMoveRects.Add((false, Rect));
        _selectionLayer = layer;
    }

    internal TileSelectionHandler(Tilegrid grid, Rectangle rectPixels, char[,] toMove, SelectionLayer layer) {
        (Grid, Rect) = (grid, rectPixels);
        _toMoveRects.Add((false, Rect));

        _toMove = toMove;
        _orig = (char[,]) Grid.Tiles.Clone();
        _selectionLayer = layer;
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

    public char[,] GetSelectedTiles() => _toMove ?? CreateToMove();

    public IHistoryAction DeleteSelf() {
        // todo: use this when possible for less memory usage
        //return new TileRectChangeAction('0', Rect.Div(8), Grid);
        var toMove = _toMove ?? CreateToMove();
        var (w, h) = (toMove.GetLength(0), toMove.GetLength(1));
        var mask = new bool[w, h];
        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                mask[x, y] = toMove[x, y] != '0';
            }
        }

        return new RemoveTilesAction(Grid, Rect.Div(8).Location, mask);
    }

    private void HandleToMove(char[,] toMove, Rectangle rect, bool exclude, int offX, int offY) {
        for (int x = rect.X; x < rect.Right; x++)
            for (int y = rect.Y; y < rect.Bottom; y++)
                toMove[x - offX, y - offY] = exclude ? '0' : Grid.SafeTileAt(x, y);
    }

    public IHistoryAction MoveBy(Vector2 offset) {
        var tileOffset = (offset / 8).ToPoint();

        if (tileOffset is { X: 0, Y: 0 })
            return new MergedAction();

        ConsumeTilesIfNeeded();

        var action = new TileRectMoveAction(Grid, Rect.Div(8), _orig!, _toMove, tileOffset)
            .WithHook(
            onApply: () => MoveRects(tileOffset),
            onUndo: () => MoveRects(new(-tileOffset.X, -tileOffset.Y)));

        return action;
    }

    private void MoveRects(Point tileOffset) {
        var moveOffset = tileOffset.ToVector2() * 8;
        Rect = Rect.MovedBy(moveOffset);
        for (int i = 0; i < _toMoveRects.Count; i++)
            _toMoveRects[i] = (_toMoveRects[i].Exclude, _toMoveRects[i].Item2.MovedBy(moveOffset));
    }

    private void ConsumeTilesIfNeeded() {
        if (_orig is null) {
            _orig ??= (char[,]) Grid.Tiles.Clone();
            var rect = Rect.Div(8);

            var toMove = _toMove ??= CreateToMove();

            for (int x = rect.X; x < rect.Right; x++)
                for (int y = rect.Y; y < rect.Bottom; y++)
                    if (x >= 0 && y >= 0 && x < Grid.Width && y < Grid.Height
                        && toMove[x - rect.X, y - rect.Y] != '0') {
                        _orig[x, y] = '0';
                    }
        }
    }

    private char[,] CreateToMove() {
        var rect = Rect.Div(8);
        var (w, h) = (rect.Width, rect.Height);

        var toMove = new char[w, h];
        toMove.Fill('0');
        foreach (var (exclude, r) in _toMoveRects)
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

        if (_toMove is not { } toMove)
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
            item.Render(SpriteRenderCtx.Default(true));
    }
    
    public void RenderHollow(Color c) {
        foreach (var item in GetSprites(c, hollow: true))
            item.Render(SpriteRenderCtx.Default(true));
    }

    internal IEnumerable<RectangleSprite> GetSprites(Color c, Vector2? pos = null, bool hollow = false) {
        var rect = Rect.Div(8);

        Vector2 rPos = pos ?? new(rect.X * 8, rect.Y * 8);

        _toMove ??= CreateToMove();

        var fillColor = hollow ? Color.Transparent : c * 0.3f;

        if (_toMove is { } toMove)
            // if we've moved the tiles, make sure to render ToMove instead of the current grid,
            // as otherwise we might render an outline on a tile that's within range, but not actually selected
            for (int x = 0; x < toMove.GetLength(0); x++)
                for (int y = 0; y < toMove.GetLength(1); y++)
                    if (toMove[x, y] != '0')
                        yield return ISprite.OutlinedRect(new(x * 8 + rPos.X, y * 8 + rPos.Y), 8, 8, fillColor, c * 0.7f);
        //yield return ISprite.OutlinedRect(Rect, Color.Pink * 0.1f, Color.Pink);
    }

    public void MergeWith(Rectangle rectPixels, bool exclude) {
        rectPixels = rectPixels.Div(8).AddSize(1, 1).Mult(8);

        var merged = RectangleExt.Merge(Rect, rectPixels);
        Rect = merged;
        _toMoveRects.Add((exclude, rectPixels));

        if (_toMove is { }) {
            _toMove = null!;
            _orig = null;
        }
    }

    public IHistoryAction? TryResize(Point delta) {
        return null;
    }

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
        return null;
    }

    public BinaryPacker.Element? PackParent() {
        var toMove = _toMove ?? CreateToMove();

        return new("tiles") {
            Attributes = new() {
                ["text"] = Tilegrid.GetSaveString(toMove),
                ["w"] = toMove.GetLength(0),
                ["h"] = toMove.GetLength(1),
                ["x"] = Rect.X,
                ["y"] = Rect.Y,
            }
        };
    }

    public void RenderSelection(Color c) => Render(c);
    public void RenderSelectionHollow(Color c) => RenderHollow(c);

    public void ClearCollideCache() {

    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
        SelectionContextWindowRegistry.OpenPopup(this, selections);
    }

    public IHistoryAction PlaceClone(Room room) {
        return new TilePasteAction(new(_toMove), Grid, Rect.Div(8).Location);
    }

    public IHistoryAction PlaceCloneAt(Room room, Point tilePos) {
        return new TilePasteAction(new(_toMove), Grid, tilePos);
    }

    public IHistoryAction? TryFlipHorizontal() {
        ConsumeTilesIfNeeded();

        var newToMove = _toMove.CreateFlippedHorizontally();

        var action = new TileSwapAction(Grid, Rect.Div(8), _orig!, newToMove);
        _toMove = newToMove;

        return action;
    }

    public IHistoryAction? TryFlipVertical() {
        ConsumeTilesIfNeeded();

        var newToMove = _toMove.CreateFlippedVertically();

        var action = new TileSwapAction(Grid, Rect.Div(8), _orig!, newToMove);
        _toMove = newToMove;

        return action;
    }

    public IHistoryAction? TryRotate(RotationDirection dir) {
#warning TODO: Tile Rotations
        return null;
    }

    public SelectionLayer Layer => _selectionLayer;

    Rectangle ISelectionHandler.Rect => Rect;

    Rectangle ISelectionCollider.Rect => Rect;
}
