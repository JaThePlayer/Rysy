using Rysy.Graphics;

namespace Rysy.History;

public record TileSwapAction(Tilegrid Grid, Rectangle Rect, char[,] OrigTiles, char[,] NewTiles, Point NewTileOffset) : IHistoryAction {
    char[,] _oldTiles;

    public bool Apply(Map map) {
        if (Rect.Width < 1 || Rect.Height < 1)
            return false;

        _oldTiles = new char[Rect.Width, Rect.Height];

        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = NewTiles.GetOrDefault(x - Rect.X + NewTileOffset.X, y - Rect.Y + NewTileOffset.Y, '0');
                if (c == '0') {
                    // instead of replacing with air, bring back the old value to not replace tiles with air when not needed
                    Grid.SafeReplaceTile(OrigTiles.GetOrDefault(x, y, '0'), x, y, out _oldTiles[x - Rect.X, y - Rect.Y]);
                    continue;
                }

                Grid.SafeReplaceTile(c, x, y, out _oldTiles[x - Rect.X, y - Rect.Y]);
            }

        Grid.RenderCacheToken?.Invalidate();
        return true;
    }

    public void Undo(Map map) {
        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = _oldTiles[x - Rect.X, y - Rect.Y];
                Grid.SafeSetTile(c, x, y);
            }
    }
}
