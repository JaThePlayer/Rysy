using Rysy.Graphics;

namespace Rysy.History;

public record class RemoveTilesAction(Tilegrid Grid, Point StartPos, bool[,] ToRemove) : IHistoryAction {
    char[,] _orig;

    public bool Apply(Map map) {
        var (w, h) = (ToRemove.GetLength(0), ToRemove.GetLength(1));
        var (ox, oy) = StartPos;
        _orig = new char[w, h];

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (ToRemove[x, y])
                    Grid.SafeReplaceTile('0', x + ox, y + oy, out _orig[x, y]);
            }
        }

        return true;
    }

    public void Undo(Map map) {
        var (w, h) = (ToRemove.GetLength(0), ToRemove.GetLength(1));
        var (ox, oy) = StartPos;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (ToRemove[x, y])
                    Grid.SafeSetTile(_orig[x, y], x + ox, y + oy);
            }
        }
    }
}
