using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.History;

internal record class TilegridMoveActionAfterResize(Tilegrid Grid, char[,] PreResizeTiles, int OffX, int OffY) : IHistoryAction {
    public bool Apply() {
        if (OffX == 0 && OffY == 0) 
            return false;

        var tiles = Grid.Tiles;
        for (int x = 0; x < tiles.GetLength(0); x++) {
            for (int y = 0; y < tiles.GetLength(1); y++) {
                if (PreResizeTiles.TryGet(x - OffX, y - OffY, out var c)) {
                    tiles[x, y] = c;
                } else {
                    tiles[x, y] = '0';
                }
            }
        }

        return true;
    }

    public void Undo() {
        var tiles = Grid.Tiles;
        for (int x = 0; x < tiles.GetLength(0); x++) {
            for (int y = 0; y < tiles.GetLength(1); y++) {
                Grid.SafeSetTile(tiles[x, y], x - OffX, y - OffY);
            }
        }
    }
}
