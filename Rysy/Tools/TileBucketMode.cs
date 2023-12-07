using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Tools; 

public class TileBucketMode : TileMode {
    public TileBucketMode(TileTool tool) : base(tool)
    {
    }

    public override string Name => "bucket";
    
    public override void Render(Camera camera, Room room) {
        var tile = Tool.GetMouseTilePos(camera, room);
        var mouse = tile.Mult(8);

        var changed = GetChangedTilesAt(room, tile, Tool.TileOrAlt(), out var filled, cap: 10_000);
        foreach (var p in changed) {
            ISprite.Rect(new Rectangle(p.X * 8, p.Y * 8, 8, 8), Tool.DefaultColor * 0.3f).Render();
        }

        if (changed.Count == 0) {
            ISprite.OutlinedRect(new Rectangle(mouse.X, mouse.Y, 8, 8), Color.Transparent, Tool.DefaultColor).Render();
        }
    }

    public override void Update(Camera camera, Room room) {
        if (!Tool.Input.Mouse.Left.Clicked()) {
            return;
        }

        var tiles = Tool.GetGrid(room).Tiles;
        var pos = Tool.GetMouseTilePos(camera, room);

        if (!tiles.TryGet(pos.X, pos.Y, out var replacedTile)) {
            return;
        }

        var tile = Tool.TileOrAlt();
        var changedTiles = GetChangedTilesAt(room, pos, tile, out _);
        
        Tool.History.ApplyNewAction(new History.TileBulkChangeAction(tile, changedTiles, Tool.GetGrid(room)));
    }

    private HashSet<Point> GetChangedTilesAt(Room room, Point startPos, char newTile, out bool finishedFilling, int? cap = null) {
        var changedTiles = new HashSet<Point>();
        var tiles = Tool.GetGrid(room).Tiles;
        finishedFilling = true;

        if (!tiles.TryGet(startPos.X, startPos.Y, out var replacedTile)) {
            return changedTiles;
        }

        if (replacedTile == newTile) {
            return changedTiles;
        }
        
        finishedFilling = Utils.FloodFill(startPos.X, startPos.Y,
            (x, y) => !changedTiles.Contains(new(x, y)) && tiles.TryGet(x, y, out var id) && id == replacedTile,
            (x, y) => {
                changedTiles.Add(new(x, y)); 
            }, cap);

        return changedTiles;
    }

    public override void CancelInteraction() {
    }

    public override void Init() {
    }
}