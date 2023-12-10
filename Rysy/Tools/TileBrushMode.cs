using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public class TileBrushMode : TileMode {
    public override string Name => "brush";

    private MouseDragGesture<BrushStrokeData> _dragGesture;
    
    private char StrokeTile => Tool.TileOrAlt(_dragGesture.Shift);
    
    public override void Render(Camera camera, Room room) {
        var mouse = room.WorldToRoomPos(camera, Tool.Input.Mouse.Pos.ToVector2()).Snap(8).ToPoint();

        Tool.RenderTileRectangle(mouse.ToVector2(), 1, 1, hollow: false);
        ISprite.OutlinedRect(new Rectangle(mouse.X, mouse.Y, 8, 8), Color.Transparent, Tool.DefaultColor).Render();
    }

    public override void Update(Camera camera, Room room) {
        _dragGesture.Update(out var continueStroke, out var endStroke, out var prevPos, out var data);

        if (continueStroke) {
            var grid = Tool.GetGrid(room);
            
            data!.FakeTiles ??= grid.Tiles.CreateClone();
        
            var anyChanged = false;
            
            var curr = Tool.GetMouseTilePos(camera, room);
            var prev = Tool.GetMouseTilePos(camera, room, fakeMousePos: prevPos);
        
            foreach (var p in TileUtils.GetLineGridIntersection(prev, curr)) {
                if (data.FakeTiles.TryReplace(p.X, p.Y, StrokeTile, out _)) {
                    anyChanged = true;
                    data.ChangedTilePositions.Add(p);
                }
            }

            // update the tilegrid's cached sprites to reflect this fake tile info.
            if (anyChanged && grid.Autotiler is { }) {
                foreach (var p in data.ChangedTilePositions) {
                    grid.Autotiler.UpdateSpriteList(grid.GetSprites(), data.FakeTiles, p.X, p.Y, true);
                    grid.RenderCacheToken?.Invalidate();
                }
            }
        }
        
        if (endStroke) {
            if (data?.ChangedTilePositions is not { Count: > 0 } stroke)
                return;

            var grid = Tool.GetGrid(room);
            var altGrid = Tool.GetSecondGrid(room);
            var tile = StrokeTile;

            Tool.History.ApplyNewAction(new MergedAction(
                new TileBulkChangeAction(tile, stroke, grid),
                altGrid is { } ? new TileBulkChangeAction(tile, stroke, altGrid) : null
            ));
        
            ClearStrokeData(room);
        }
    }

    public override void CancelInteraction() {
        ClearStrokeData(null);
    }

    public override void Init() {
        _dragGesture = new(Tool.Input);
    }
    
    private void ClearStrokeData(Room? room) {
        _dragGesture.CancelStroke();

        ClearTilegridSpriteCache(room);
    }

    public TileBrushMode(TileTool tool) : base(tool)
    {
    }
    
    // data used by the mouse drag gesture
    internal class BrushStrokeData {
        public readonly HashSet<Point> ChangedTilePositions = new();
    
        // stores a copy of the current room's tilegrid, with all of the changes from this brush stroke.
        public char[,]? FakeTiles;
    }
}