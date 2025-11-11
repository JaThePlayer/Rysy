using Rysy.Graphics;

namespace Rysy.History;

public class TilePasteAction : IHistoryAction {
    public readonly Tilegrid Source;
    public readonly Tilegrid Destination;
    public readonly Point Pos;

    private char[,] _old;

    public TilePasteAction(Tilegrid source, Tilegrid destination, Point pos) {
        Source = source;
        Destination = destination;
        Pos = pos;
    }

    public bool Apply(Map map) {
        var (w, h) = (Source.Width, Source.Height);
        var (xOff, yOff) = Pos;

        _old = new char[w, h];

        bool changed = false;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var c = Source.Tiles[x, y];
                if (c != '0')
                    changed |= Destination.SafeReplaceTile(c, x + xOff, y + yOff, out _old[x, y]);
                else
                    _old[x, y] = Destination.SafeTileAt(x + xOff, y + yOff);
            }
        }

        return changed;
    }

    public void Undo(Map map) {
        var (w, h) = (Source.Width, Source.Height);
        var (xOff, yOff) = Pos;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                Destination.SafeReplaceTile(_old[x, y], x + xOff, y + yOff, out _);
            }
        }
    }
}
