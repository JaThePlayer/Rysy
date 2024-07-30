using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Collections;

namespace Rysy.Tools; 

public class TileBucketMode : TileMode {
    public TileBucketMode(TileTool tool) : base(tool)
    {
    }

    public override string Name => "bucket";

    private WrappedBitArray? _prevChangedTiles;
    
    public override void Render(Camera camera, Room room) {
        var tile = Tool.GetMouseTilePos(camera, room);
        var mouse = tile.Mult(8);

        var width = Tool.GetGrid(room).Width;
        if (!_prevChangedTiles?.Get2d(tile.X, tile.Y, width) ?? true) {
            _prevChangedTiles?.ReturnToPool();
            _prevChangedTiles = GetChangedTilesAt(room, tile, Tool.TileOrAlt(), out var filled);
        }

        var tiles = _prevChangedTiles.Value;
        var color = Tool.DefaultColor * 0.3f;
        for (int i = 0; i < tiles.Length; i++) {
            if (!tiles.Get(i))
                continue;

            var w = 8;
            var (x, y) = tiles.Get2dLoc(i, width);
            var maxI = i + width - x;
            // Merge rectangles horizontally:
            while (++i < maxI && tiles.Get(i)) {
                w += 8;
            }
            // we overshoot inside the while loop, let's cancel that out
            i--;
            
            GFX.Batch.Draw(GFX.Pixel, new Rectangle(x * 8, y * 8, w, 8), null, color);
        }

        if (tiles.Length == 0) {
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
        
        Tool.History.ApplyNewAction(new History.TileBulkBitArrayChangeAction(tile, changedTiles.ToBitArray(), Tool.GetGrid(room)));
        changedTiles.ReturnToPool();
        
        ClearPrevChangedTiles();
    }

    private WrappedBitArray GetChangedTilesAt(Room room, Point startPos, char newTile, out bool finishedFilling, int? cap = null, Action<int, int>? onSet = null) {
        var tiles = Tool.GetGrid(room).Tiles;
        var changeMask = WrappedBitArray.Rent(tiles.Length);
        var w = tiles.GetLength(0);
        
        finishedFilling = true;

        if (!tiles.TryGet(startPos.X, startPos.Y, out var replacedTile)) {
            return changeMask;
        }

        if (replacedTile == newTile) {
            return changeMask;
        }
        
        finishedFilling = TileUtils.FloodFill(startPos.X, startPos.Y,
            (x, y) => tiles.TryGet(x, y, out var id) && id == replacedTile && !changeMask.Get2d(x, y, w),
            (x, y) => {
                changeMask.Set2d(x, y, w, true);
                onSet?.Invoke(x, y);
            }, cap);

        return changeMask;
    }

    public override void CancelInteraction() {
        ClearPrevChangedTiles();
    }

    private void ClearPrevChangedTiles() {
        _prevChangedTiles?.ReturnToPool();
        _prevChangedTiles = null;
    }

    public override void Init() {
    }
}