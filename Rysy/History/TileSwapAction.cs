﻿using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.History;

public record class TileSwapAction(Tilegrid Grid, Rectangle Rect, char[,] OrigTiles, char[,] NewTiles) : IHistoryAction {
    char[,] OldTiles;

    public bool Apply() {
        if (Rect.Width < 1 || Rect.Height < 1)
            return false;

        OldTiles = new char[Rect.Width, Rect.Height];

        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = NewTiles[x - Rect.X, y - Rect.Y];
                if (c == '0') {
                    // instead of replacing with air, bring back the old value to not replace tiles with air when not needed
                    Grid.SafeReplaceTile(OrigTiles.GetOrDefault(x, y, '0'), x, y, out OldTiles[x - Rect.X, y - Rect.Y]);
                    continue;
                }

                Grid.SafeReplaceTile(c, x, y, out OldTiles[x - Rect.X, y - Rect.Y]);
            }

        Grid.RenderCacheToken?.Invalidate();
        return true;
    }

    public void Undo() {
        for (int x = Rect.X; x < Rect.Right; x++)
            for (int y = Rect.Y; y < Rect.Bottom; y++) {
                var c = OldTiles[x - Rect.X, y - Rect.Y];
                //if (c == '0')
                //    continue;
                Grid.SafeSetTile(c, x, y);
            }
    }
}
