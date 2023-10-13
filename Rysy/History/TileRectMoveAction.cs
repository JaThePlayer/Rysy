using Rysy.Graphics;

namespace Rysy.History;
internal sealed record class TileRectMoveAction(Tilegrid Grid, Rectangle Rect, char[,] Orig, char[,] ToMove, Point Offset) : IHistoryAction {
    char[,] Old;

    public bool Apply() {
        var ox = Offset.X;
        var oy = Offset.Y;

        if (Rect.Width < 1 || Rect.Height < 1)
            return false;

        Old = Grid.Tiles; // todo: avoid holding a reference to the entire tile grid...

        Grid.Tiles = (char[,]) Orig.Clone();

        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = ToMove[x - Rect.X, y - Rect.Y];
                if (c == '0')
                    continue;

                Grid.SafeReplaceTile(c, x + ox, y + oy, out char orig);
            }

        Grid.RenderCacheToken?.Invalidate();
        return true;
    }

    public void Undo() {
        Grid.Tiles = Old;

        Grid.RenderCacheToken?.Invalidate();
    }
}
