using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools;

public class BrushTool : TileTool {
    public override string Name => "brush";

    private MouseDragGesture<BrushStrokeData> _dragGesture;
    
    private char StrokeTile => _dragGesture.Shift ? '0' : Tile;

    public override void Init() {
        base.Init();

        _dragGesture = new(Input);
    }

    public override void Render(Camera camera, Room room) {
        var mouse = room.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).Snap(8).ToPoint();

        RenderTiles(mouse.ToVector2(), 1, 1);
        ISprite.OutlinedRect(new Rectangle(mouse.X, mouse.Y, 8, 8), Color.Transparent, DefaultColor).Render();
    }

    public override void Update(Camera camera, Room room) {
        base.Update(camera, room);
        
        _dragGesture.Update(out var continueStroke, out var endStroke, out var prevPos, out var data);

        if (continueStroke) {
            var grid = GetGrid(room);
            
            data!.FakeTiles ??= grid.Tiles.CreateClone();
        
            var anyChanged = false;
            
            var curr = GetMouseTilePos(camera, room);
            var prev = GetMouseTilePos(camera, room, fakeMousePos: prevPos);
        
            foreach (var p in Utils.GetLineGridIntersection(prev, curr)) {
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

            var grid = GetGrid(room);
            var altGrid = GetSecondGrid(room);
            var tile = StrokeTile;

            History.ApplyNewAction(new MergedAction(
                new TileBulkChangeAction(tile, stroke, grid),
                altGrid is { } ? new TileBulkChangeAction(tile, stroke, altGrid) : null
            ));
        
            ClearStrokeData(room);
        }
    }
    
    public override void CancelInteraction() {
        base.CancelInteraction();

        ClearStrokeData(null);
    }
    
    private void ClearStrokeData(Room? room) {
        _dragGesture.CancelStroke();

        room ??= EditorState.CurrentRoom;
        if (room is null)
            return;
        GetGrid(room)?.ClearSpriteCache();
        GetSecondGrid(room)?.ClearSpriteCache();
    }
}

// data used by the mouse drag gesture
internal class BrushStrokeData {
    public readonly HashSet<Point> ChangedTilePositions = new();
    
    // stores a copy of the current room's tilegrid, with all of the changes from this brush stroke.
    public char[,]? FakeTiles;
}
