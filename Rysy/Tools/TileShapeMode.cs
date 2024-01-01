using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public abstract class TileShapeMode : TileMode {
    protected abstract IEnumerable<Point> GetChangedTileLocations(Point start, Point current);
    protected abstract IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid);

    protected TileShapeMode(TileTool tool) : base(tool) {
    }
    
    private MouseDragGesture<ShapeData> _dragGesture;
    
    public override void Render(Camera camera, Room room) {
        var mousePos = Tool.GetMouseTilePos(camera, room);
        var startPos = _dragGesture.Data?.StartPos ?? mousePos;

        if (!_dragGesture.Begun) {
            Tool.RenderTileRectangle(mousePos.Mult(8).ToVector2(), 1, 1, hollow: false);
        } else {
            var (outline, fill) = Tool.GetSelectionColor(RectangleExt.FromPoints(mousePos, startPos).MultSize(8));

            ISprite.LineFloored(mousePos.ToVector2().Add(0.5f, 0.5f) * 8, startPos.ToVector2().Add(0.5f, 0.5f) * 8, outline).Render(SpriteRenderCtx.Default());
            
            Tool.DrawSelectionRect(new Rectangle(startPos.X * 8, startPos.Y * 8, 8, 8));
            Tool.DrawSelectionRect(new Rectangle(mousePos.X * 8, mousePos.Y * 8, 8, 8));
        }
    }

    public override void Update(Camera camera, Room room) {
        _dragGesture.Update(out var continueStroke, out var endStroke, out var lastMousePos, out var data);

        if (continueStroke || endStroke) {
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
        
        if (endStroke) {
            var endPos = Tool.GetMouseTilePos(camera, room);
            var startPos = data!.StartPos!.Value;
            var tile = Tool.TileOrAlt(_dragGesture.Shift);
            
            Tool.History.ApplyNewAction(new MergedAction(
                CreateAction(tile, startPos, endPos, Tool.GetGrid(room)),
                Tool.GetSecondGrid(room) is {} second ? CreateAction(tile, startPos, endPos, second) : null
            ));
        }
    }

    public override void CancelInteraction() {
        _dragGesture.CancelStroke();
        ClearTilegridSpriteCache();
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
}