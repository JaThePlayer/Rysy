using Rysy.Graphics;

namespace Rysy.History;

/// <summary>
/// An action where one tile gets changed in a tile grid. Calls SafeSetTile.
/// </summary>
public record class TileChangeAction(char ID, int X, int Y, Tilegrid Grid, Tilegrid? Grid2 = null) : IHistoryAction {
    private char lastID;
    private char lastID2;

    public bool Apply() {
        lastID = Grid.SafeTileAt(X, Y);
        lastID2 = Grid2?.SafeTileAt(X, Y) ?? '0';
        return Grid.SafeSetTile(ID, X, Y) | (Grid2?.SafeSetTile(ID, X, Y) ?? false);
    }

    public void Undo() {
        Grid.SafeSetTile(lastID, X, Y);
        Grid2?.SafeSetTile(lastID2, X, Y);
    }
}

public record class TileRectChangeAction(char ID, Rectangle Rectangle, Tilegrid Grid, Tilegrid? Grid2 = null) : IHistoryAction {
    private char[,]? oldTiles, oldTiles2;

    public unsafe bool Apply() {
        var w = Rectangle.Width;
        var h = Rectangle.Height;
        var sx = Rectangle.X;
        var sy = Rectangle.Y;
        var id = ID;

        var oldGrid = new char[w, h];
        var oldGrid2 = Grid2 is { } ? new char[w, h] : null;

        bool changed = false;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var gx = x + sx;
                var gy = y + sy;
                changed |= Grid.SafeReplaceTile(id, gx, gy, out oldGrid[x, y]);

                if (oldGrid2 is { }) {
                    changed |= Grid2!.SafeReplaceTile(id, gx, gy, out oldGrid2[x, y]);
                }
            }
        }

        oldTiles = oldGrid;
        oldTiles2 = oldGrid2;

        return changed;
    }

    public void Undo() {
        var w = Rectangle.Width;
        var h = Rectangle.Height;
        var sx = Rectangle.X;
        var sy = Rectangle.Y;
        var oldGrid = oldTiles;
        var oldGrid2 = oldTiles2;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var gx = x + sx;
                var gy = y + sy;
                Grid.SafeSetTile(oldGrid![x, y], gx, gy);
                if (Grid2 is { }) {
                    Grid2.SafeSetTile(oldGrid2![x, y], gx, gy);
                }
            }
        }
    }
}