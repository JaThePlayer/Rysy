using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.History;

internal sealed record TileRectMoveAction(Tilegrid Grid, Rectangle Rect, char[,] Orig, char[,] ToMove, Point Offset) : IHistoryAction {
    char[,] _old;

    public bool Apply(Map map) {
        var ox = Offset.X;
        var oy = Offset.Y;

        if (Rect.Width < 1 || Rect.Height < 1)
            return false;

        _old = Grid.Tiles; // todo: avoid holding a reference to the entire tile grid...

        var cachedSprites = Grid.CachedSprites;
        
        Grid.Tiles = (char[,]) Orig.Clone();

        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = ToMove[x - Rect.X, y - Rect.Y];
                if (c == '0')
                    continue;

                Grid.SafeReplaceTile(c, x + ox, y + oy, out char orig);
            }

        UpdateCache(cachedSprites);

        return true;
    }

    private void UpdateCache(AutotiledSpriteList? cachedSprites) {
        if (cachedSprites is not { }) {
            return;
        }
        
        // Make use of partial re-autotiling, even though we're swapping out tilegrids,
        // which normally causes a full re-autotile - in this case, we know exactly what changed!

        Grid.CachedSprites = cachedSprites;
        var rect = Rect;
        rect.Offset(Offset);
        Grid.Autotiler?.BulkUpdateSpriteList(cachedSprites, Grid.Tiles, Rect.EnumerateGridLocations(), true);
        Grid.Autotiler?.BulkUpdateSpriteList(cachedSprites, Grid.Tiles, rect.EnumerateGridLocations(), true);
    }

    public void Undo(Map map) {
        var cachedSprites = Grid.CachedSprites;
        Grid.Tiles = _old;

        UpdateCache(cachedSprites);
    }
}

/*
More memory efficient implementation, however it does not work well for the selection tool 
- resting moved tiles on top of other tiles, then moving again will remove the unselected tiles
internal sealed record class TileRectMoveAction(Tilegrid Grid, Rectangle Rect, char[,] ToMove, Point Offset) : IHistoryAction {
    private char[,] _cutoutTiles;
    private char[,] _movedTiles;

    public bool Apply() {
        var ox = Offset.X;
        var oy = Offset.Y;

        if (Rect.Width < 1 || Rect.Height < 1)
            return false;

        _cutoutTiles = new char[Rect.Width, Rect.Height];
        for (int x = Rect.X; x < Rect.Right; x++)
        for (int y = Rect.Y; y < Rect.Bottom; y++) {
            var nx = x - Rect.X;
            var ny = y - Rect.Y;

            _cutoutTiles[nx, ny] = Grid.SafeTileAt(x, y);

            var c = ToMove[nx, ny];
            if (c != '0') {
                Grid.SafeSetTile('0', x, y);
            }
        }

        _movedTiles = new char[Rect.Width, Rect.Height];
        for (int x = Rect.X; x < Rect.Right; x++)
        for (int y = Rect.Y; y < Rect.Bottom; y++) {
            var nx = x - Rect.X;
            var ny = y - Rect.Y;

            var c = ToMove[nx, ny];
            if (c == '0') {
                _movedTiles[nx, ny] = Grid.SafeTileAt(x + ox, y + oy);
                continue;
            }

            Grid.SafeReplaceTile(c, x + ox, y + oy, out _movedTiles[nx, ny]);
        }

        Grid.RenderCacheToken?.Invalidate();
        return true;
    }

    public void Undo() {
        var ox = Offset.X;
        var oy = Offset.Y;
        
        for (int x = Rect.X; x < Rect.Right; x++)
        for (int y = Rect.Y; y < Rect.Bottom; y++) {
            var nx = x - Rect.X;
            var ny = y - Rect.Y;

            var c = _movedTiles[nx, ny];

            Grid.SafeReplaceTile(c, x + ox, y + oy, out _);
        }

        for (int x = Rect.X; x < Rect.Right; x++)
        for (int y = Rect.Y; y < Rect.Bottom; y++) {
            var nx = x - Rect.X;
            var ny = y - Rect.Y;

            var c = _cutoutTiles[nx, ny];

            Grid.SafeReplaceTile(c, x, y, out _);
        }

        Grid.RenderCacheToken?.Invalidate();
    }
}*/