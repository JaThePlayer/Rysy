﻿using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
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
    (char Id, Point A, Point B, Tilegrid Grid) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, TileUtils.GetLineGridIntersection(A, B).GetEnumerator(), out _oldChars);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(TileUtils.GetLineGridIntersection(A, B).GetEnumerator(), _oldChars);
        
        _oldChars.Clear();
    }
}

public record TileCircleChangeAction
    (char Id, Point A, Point B, Tilegrid Grid, bool Hollow) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;

    private IEnumerable<Point> GetPoints() => Hollow
        ? TileUtils.GetHollowCircleGridIntersection(A, B)
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
    (char Id, Point A, Point B, Tilegrid Grid, bool Hollow) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldChars;

    private IEnumerable<Point> GetPoints() => Hollow
        ? TileUtils.GetHollowEllipseGridIntersection(A, B)
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

public record TileBulkChangeAction(char Id, HashSet<Point> Points, Tilegrid Grid) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldTiles;
    
    public bool Apply(Map map) {
        var anyChanged = Grid.BulkReplaceTiles(Id, Points.GetEnumerator(), out _oldTiles, locationCountHint: Points.Count);

        return anyChanged;
    }

    public void Undo(Map map) {
        Grid.BulkReplaceTiles(Points.GetEnumerator(), _oldTiles);
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

public record TileRectChangeAction(char Id, Rectangle Rectangle, Tilegrid Grid, bool Hollow) : IHistoryAction {
    private Tilegrid.BulkReplaceDelta _oldTiles;

    public bool Apply(Map map) {
        var count = Rectangle.Width * Rectangle.Height;
        if (Hollow) {
            return Grid.BulkReplaceTiles(Id, Rectangle.EnumerateGridEdgeLocations(), out _oldTiles, locationCountHint: count);
        }
        
        return Grid.BulkReplaceTiles(Id, Rectangle.EnumerateGridLocations(), out _oldTiles, locationCountHint: count);
    }

    public void Undo(Map map) {
        if (Hollow) {
            Grid.BulkReplaceTiles(Rectangle.EnumerateGridEdgeLocations(), _oldTiles);
        } else {
            Grid.BulkReplaceTiles(Rectangle.EnumerateGridLocations(), _oldTiles);
        }
        _oldTiles.Clear();
    }
}