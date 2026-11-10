using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public abstract class TileShapeMode : TileMode {
    protected abstract IEnumerable<Point> GetChangedTileLocations(Point start, Point current);
    protected abstract IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid);

    protected TileShapeMode(TileTool tool) : base(tool) {
        _cachedthickness = Persistence.Instance.Get<int>("tileToolThickness", 1);
    }
    
    private MouseDragGesture<ShapeData> _dragGesture;

    // For the purposes of displaying overlays while dragging, this uses a thickness value that tools can override if they are not subject to thickness (eg not hollow).
    internal static int _cachedthickness;
    internal virtual int _thickness => _cachedthickness;
    
    public override void Render(Camera camera, Room room) {
        var mousePos = Tool.GetMouseTilePos(camera, room);
        var startPos = _dragGesture.Data?.StartPos ?? mousePos;
        var gridSize = GridSize;

        int offset = ((_thickness) / 2) * gridSize, size = gridSize * _thickness;
        
        if (!_dragGesture.Begun) {
            Tool.RenderTileRectangle(mousePos.Mult(gridSize).ToVector2() - Vector2.One * offset, _thickness, _thickness, hollow: false);
        } else {
            var (outline, fill) = Tool.GetSelectionColor(RectangleExt.FromPoints(mousePos, startPos).MultSize(gridSize));

            float lineadd = (_thickness % 2 > 0) ? 0.5f : 0f;
            Vector2 _mousePos = mousePos.ToVector2().Add(lineadd,lineadd), _startPos = startPos.ToVector2().Add(lineadd,lineadd);
            ISprite.LineFloored(_mousePos * gridSize, _startPos * gridSize, outline).Render(SpriteRenderCtx.Default());

            Tool.DrawSelectionRect(camera, new Rectangle(startPos.X * gridSize - offset, startPos.Y * gridSize - offset, size, size));
            Tool.DrawSelectionRect(camera, new Rectangle(mousePos.X * gridSize - offset, mousePos.Y * gridSize - offset, size, size));
        }
    }

    public override void Update(Camera camera, Room room) {
        _dragGesture.Update(out var continueStroke, out var endStroke, out var lastMousePos, out var data);
        
        // using the scroll wheel as a keybind fails to change the thickness, but does seem to set _thicknessChanged for some reason.
        // this somehow produces a crash fixable by making sure data isn't null when _thicknessChanged is true, even though _thicknessChanged seems to only be true while data isn't null.
        if (continueStroke || endStroke || (_thicknessChanged && data is {})) {
            data!.StartPos ??= Tool.GetMouseTilePos(camera, room, fakeMousePos: _dragGesture.StartingPos);

            var grid = Tool.GetGrid(room);
            data!.FakeTiles = grid.Tiles.CreateClone();
            
            var curr = Tool.GetMouseTilePos(camera, room);
            var prev = data.StartPos!.Value;
            var tile = Tool.TileOrAlt(_dragGesture.Shift);
            var changedTilePositions = new List<Point>();
            
            foreach (var p in GetChangedTileLocations(prev, curr)) {
                if (data.FakeTiles.TryReplace(p.X, p.Y, tile, out _)) {
                    changedTilePositions.Add(p);
                }
            }
            
            // update the tilegrid's cached sprites to reflect this fake tile info.
            if (grid.Autotiler is { }) {
                grid.Autotiler.BulkUpdateSpriteList(grid.GetSprites(), data.FakeTiles, changedTilePositions.Concat(data.LastChangedTiles).GetEnumerator(), true);
                grid.RenderCacheToken?.Invalidate();
            }

            data.LastChangedTiles = changedTilePositions;
        }

        _thicknessChanged = false;
        
        if (endStroke) {
            var endPos = Tool.GetMouseTilePos(camera, room);
            var startPos = data!.StartPos!.Value;
            var tile = Tool.TileOrAlt(_dragGesture.Shift);
            
            Tool.History.ApplyNewAction(CreateAction(tile, startPos, endPos, Tool.GetGrid(room)));
        }
    }

    public override void CancelInteraction() {
        if (_dragGesture.Begun)
            ClearTilegridSpriteCache(Tool.ToolHandler.EditorState.CurrentRoom);
        _dragGesture.CancelStroke();
    }

    public override void Init() {
        _dragGesture = new(Tool.Input);
    }
    
    private static Rectangle SelectionRect(Point start, Point mousePos) {
        return RectangleExt.FromPoints(start, mousePos).AddSize(1, 1);
    }

    internal sealed class ShapeData {
        // stores a copy of the current room's tilegrid, with all of the changes from this brush stroke.
        public char[,]? FakeTiles;

        public List<Point> LastChangedTiles = new();

        public Point? StartPos;
    }
    
    
    public static Action ChangeThicknessBy(int amt) => () => {
        int res = Math.Clamp(_cachedthickness + amt, 1, 500);
        Persistence.Instance.Set("tileToolThickness", res);
        _cachedthickness = res;
        // forces the update to happen, which re-renders the fake tiles with the new width.
        _thicknessChanged = true;
    };
    
    private static bool _thicknessChanged;
}