using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Tools;
using System.Collections;

namespace Rysy.History;

/// <summary>
/// An action where one tile gets changed in a tile grid. Calls SafeSetTile.
/// </summary>
public record class TileChangeAction(char Id, int X, int Y, Tilegrid Grid, Tilegrid? Grid2 = null) : IHistoryAction {
    private char _lastId;
    private char _lastId2;

    public bool Apply(Map map) {
        _lastId = Grid.SafeTileAt(X, Y);
        _lastId2 = Grid2?.SafeTileAt(X, Y) ?? '0';
        return Grid.SafeSetTile(Id, X, Y) | (Grid2?.SafeSetTile(Id, X, Y) ?? false);
    }

    public void Undo(Map map) {
        Grid.SafeSetTile(_lastId, X, Y);
        Grid2?.SafeSetTile(_lastId2, X, Y);
    }
}

public record TileLineChangeAction
    (char Id, Point A, Point B, Tilegrid Grid, int Thickness = 1) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, TileUtils.GetLineGridIntersection(A, B, Thickness).GetEnumerator(), out _oldChars);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(TileUtils.GetLineGridIntersection(A, B, Thickness).GetEnumerator(), _oldChars);
        
        _oldChars.Clear();
    }
}

public record TileCircleChangeAction
    (char Id, Point A, Point B, Tilegrid Grid, bool Hollow, int Thickness = 1) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;

    private IEnumerable<Point> GetPoints() => Hollow
        ? TileUtils.GetHollowCircleGridIntersection(A, B, Thickness)
        : TileUtils.GetCircleGridIntersection(A, B);
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, GetPoints().GetResettableEnumerator(), out _oldChars);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(GetPoints().GetResettableEnumerator(), _oldChars);
        
        _oldChars.Clear();
    }
}

public record TileEllipseChangeAction
    (char Id, Point A, Point B, Tilegrid Grid, bool Hollow, int Thickness = 1) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;
    
    private IEnumerable<Point> GetPoints() => Hollow
        ? TileUtils.GetHollowEllipseGridIntersection(A, B, Thickness)
        : TileUtils.GetEllipseGridIntersection(A, B);
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, GetPoints().GetResettableEnumerator(), out _oldChars);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(GetPoints().GetResettableEnumerator(), _oldChars);
        
        _oldChars.Clear();
    }
}

public record TileBulkChangeAction(char Id, HashSet<Point> Points, Tilegrid Grid, int Thickness = 1) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldTiles;
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, TileUtils.ThickenGridLocations(Points, Thickness).GetEnumerator(), out _oldTiles, locationCountHint: Points.Count);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(TileUtils.ThickenGridLocations(Points, Thickness).GetEnumerator(), _oldTiles);
        _oldTiles.Clear();
    }
}

public record TileBulkBitArrayChangeAction(char Id, BitArray Points, Tilegrid Grid) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldTiles;
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, Points, out _oldTiles);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(Points, _oldTiles);
        
        _oldTiles.Clear();
    }
}


public record TileRectChangeAction(char Id, Rectangle Rectangle, Tilegrid Grid, bool Hollow, int Thickness = 1) : IHistoryAction
{
    private Tilegrid.BulkReplaceDelta _oldTiles;

    private IEnumerable<Point> GetHollowPoints()
    {
        var points = (IEnumerable<Point>)(Rectangle.EnumerateGridEdgeLocations());

        return TileUtils.ThickenGridLocations(points, Thickness);
    }

    public bool Apply(Map map)
    {
        return Grid.BulkReplaceTiles(Id, Hollow ? GetHollowPoints().GetEnumerator() : Rectangle.EnumerateGridLocations(), out _oldTiles, locationCountHint: Rectangle.Width * Rectangle.Height);
    }

    public void Undo(Map map)
    {
        Grid.BulkReplaceTiles(Hollow ? GetHollowPoints().GetEnumerator() : Rectangle.EnumerateGridLocations(), _oldTiles);

        _oldTiles.Clear();
    }
}
