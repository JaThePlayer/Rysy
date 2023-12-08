using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public class TileLineMode : TileMode {
    public TileLineMode(TileTool tool) : base(tool)
    {
    }

    public override string Name => "line";
    
    private MouseDragGesture<LineData> _dragGesture;
    
    public override void Render(Camera camera, Room room) {
        var mousePos = Tool.GetMouseTilePos(camera, room);
        var startPos = _dragGesture.Data?.StartPos ?? mousePos;

        if (!_dragGesture.Begun) {
            Tool.RenderTiles(mousePos.Mult(8).ToVector2(), 1, 1);
        } else {
            var (outline, fill) = Tool.GetSelectionColor(RectangleExt.FromPoints(mousePos, startPos).MultSize(8));
            
            foreach (var (x, y) in Utils.GetLineGridIntersection(mousePos, startPos)) {
                ISprite.OutlinedRect(new Rectangle(x * 8, y * 8, 8, 8), fill, outline).Render();
            }
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
            
            foreach (var p in Utils.GetLineGridIntersection(prev, curr)) {
                if (data.FakeTiles.TryReplace(p.X, p.Y, tile, out _)) {
                    changedTilePositions.Add(p);
                }
            }
            
            // update the tilegrid's cached sprites to reflect this fake tile info.
            if (grid.Autotiler is { }) {
                foreach (var p in changedTilePositions.Concat(data.LastChangedTiles)) {
                    grid.Autotiler.UpdateSpriteList(grid.GetSprites(), data.FakeTiles, p.X, p.Y, true);
                    grid.RenderCacheToken?.Invalidate();
                }
            }

            data.LastChangedTiles = changedTilePositions;
        }
        
        if (endStroke) {
            var endPos = Tool.GetMouseTilePos(camera, room);
            var startPos = data!.StartPos!.Value;
            var tile = Tool.TileOrAlt(_dragGesture.Shift);
            
            Tool.History.ApplyNewAction(new MergedAction(
                new TileLineChangeAction(tile, startPos, endPos, Tool.GetGrid(room)),
                Tool.GetSecondGrid(room) is {} second ? new TileLineChangeAction(tile, startPos, endPos, second) : null
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

    internal sealed class LineData {
        // stores a copy of the current room's tilegrid, with all of the changes from this brush stroke.
        public char[,]? FakeTiles;

        public List<Point> LastChangedTiles = new();

        public Point? StartPos;
    }
}