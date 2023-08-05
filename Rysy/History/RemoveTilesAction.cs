using Rysy.Graphics;

namespace Rysy.History;

public record class RemoveTilesAction(Tilegrid Grid, Point StartPos, bool[,] ToRemove) : IHistoryAction {
    char[,] Orig;

    public bool Apply() {
        var (w, h) = (ToRemove.GetLength(0), ToRemove.GetLength(1));
        var (ox, oy) = StartPos;
        Orig = new char[w, h];

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (ToRemove[x, y])
                    Grid.SafeReplaceTile('0', x + ox, y + oy, out Orig[x, y]);
            }
        }

        return true;
    }

    public void Undo() {
        var (w, h) = (ToRemove.GetLength(0), ToRemove.GetLength(1));
        var (ox, oy) = StartPos;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (ToRemove[x, y])
                    Grid.SafeSetTile(Orig[x, y], x + ox, y + oy);
            }
        }
    }
}
