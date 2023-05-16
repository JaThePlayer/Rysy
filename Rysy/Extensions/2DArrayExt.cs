namespace Rysy.Extensions;

public static class _2DArrayExt {
    /// <summary>
    /// Fills the given 2d array with the specified value
    /// </summary>
    public static unsafe void Fill<T>(this T[,] tiles, T value) where T : unmanaged {
        if (tiles.Length == 0)
            return;

        fixed (T* tile = &tiles[0, 0]) {
            new Span<T>(tile, tiles.Length).Fill(value);
        }
    }

    //https://stackoverflow.com/questions/6539571/how-to-resize-multidimensional-2d-array-in-c
    /// <summary>
    /// Creates a new 2d array, which is a resized version of this array.
    /// The width and height can be smaller than the original.
    /// </summary>
    public static T[,] CreateResized<T>(this T[,] original, int width, int height, T? fillWith) where T : unmanaged {
        var newArray = new T[width, height];

        if (fillWith is { } fill)
            newArray.Fill(fill);

        int mMin = Math.Min(original.GetLength(0), newArray.GetLength(0));
        int nMin = Math.Min(original.GetLength(1), newArray.GetLength(1));

        for (int i = 0; i < mMin; i++)
            Array.Copy(original, i * original.GetLength(1), newArray, i * newArray.GetLength(1), nMin);

        return newArray;
    }

    /// <summary>
    /// Gets the element at <paramref name="arr"/>[<paramref name="x"/>, <paramref name="y"/>], or <paramref name="def"/> if the index is out of bounds
    /// </summary>
    public static T GetOrDefault<T>(this T[,] arr, int x, int y, T def) {
        if (x < 0 || y < 0)
            return def;
        if (x >= arr.GetLength(0) || y >= arr.GetLength(1))
            return def;

        return arr[x, y];
    }

    public static T[,] CreateFlippedHorizontally<T>(this T[,] arr) {
        var w = arr.GetLength(0);
        var h = arr.GetLength(1);
        var flipped = new T[w, h];

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                flipped[x, y] = arr[w - x - 1, y];
            }
        }

        return flipped;
    }

    public static T[,] CreateFlippedVertically<T>(this T[,] arr) {
        var w = arr.GetLength(0);
        var h = arr.GetLength(1);
        var flipped = new T[w, h];

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                flipped[x, y] = arr[x, h - y - 1];
            }
        }

        return flipped;
    }
}
