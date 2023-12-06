using Rysy.Graphics;
using Rysy.Helpers;

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

public record class TileLineChangeAction
    (char ID, Point A, Point B, Tilegrid Grid) : IHistoryAction {
    private List<char> _oldChars;
    
    public bool Apply() {
        var id = ID;
        var anyChanged = false;
        
        _oldChars = new();
        foreach (var (x, y) in Utils.GetLineGridIntersection(A, B)) {
            anyChanged |= Grid.SafeReplaceTile(id, x, y, out var last);
            
            _oldChars.Add(last);
        }

        return anyChanged;
    }

    public void Undo() {
        var i = 0;
        foreach (var (x, y) in Utils.GetLineGridIntersection(A, B)) {
            Grid.SafeSetTile(_oldChars.ElementAtOrDefault(i), x, y);
            
            i++;
        }
        
        _oldChars.Clear();
    }
}

public record TileBulkChangeAction(char ID, HashSet<Point> Points, Tilegrid Grid) : IHistoryAction {
    private List<char> _oldChars;
    
    public bool Apply() {
        var id = ID;
        var anyChanged = false;
        
        _oldChars = new(Points.Count);
        foreach (var (x, y) in Points) {
            anyChanged |= Grid.SafeReplaceTile(id, x, y, out var last);
            
            _oldChars.Add(last);
        }

        return anyChanged;
    }

    public void Undo() {
        var i = 0;
        foreach (var (x, y) in Points) {
            Grid.SafeSetTile(_oldChars[i], x, y);
            i++;
        }
        
        _oldChars.Clear();
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