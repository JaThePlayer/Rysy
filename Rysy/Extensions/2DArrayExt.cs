using System.Diagnostics.CodeAnalysis;

namespace Rysy.Extensions;

public static class TwoDimensionalArrayExt {
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

    public static T[,] CreateClone<T>(this T[,] arr) {
        var clone = arr.Clone();
        return (T[,]) clone;
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
        if ((uint)x >= arr.GetLength(0) || (uint)y >= arr.GetLength(1))
            return def;

        return arr[x, y];
    }

    /// <summary>
    /// Tries to get the element at <paramref name="arr"/>[<paramref name="x"/>, <paramref name="y"/>]
    /// </summary>
    public static bool TryGet<T>(this T[,] arr, int x, int y, out T? val) {
        if ((uint)x >= arr.GetLength(0) || (uint)y >= arr.GetLength(1)) {
            val = default;
            return false;
        }

        val = arr[x, y];
        return true;
    }

    /// <summary>
    /// Tries to set the element at <paramref name="arr"/>[<paramref name="x"/>, <paramref name="y"/>]
    /// </summary>
    public static bool TrySet<T>(this T[,] arr, int x, int y, T val) {
        if ((uint)x >= arr.GetLength(0) || (uint)y >= arr.GetLength(1)) {
            return false;
        }

        arr[x, y] = val;
        return true;
    }
    
    public static bool TryReplace<T>(this T[,] arr, int x, int y, T val, out T? prev) where T : IEquatable<T> {
        prev = default;
        
        if ((uint)x >= arr.GetLength(0) || (uint)y >= arr.GetLength(1)) {
            return false;
        }

        ref var loc = ref arr[x, y];
        if (loc.Equals(val)) {
            return false;
        }

        prev = loc;
        loc = val;
        return true;
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

    public static T[,] CreateTrimmed<T>(this T[,] arr, T emptyValue, out int offX, out int offY) where T : IEquatable<T> {
        var (minX, minY, maxX, maxY) = (int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        for (int x = 0; x < arr.GetLength(0); x++) {
            for (int y = 0; y < arr.GetLength(1); y++) {
                var c = arr.GetOrDefault(x, y, emptyValue);
                if (!c.Equals(emptyValue)) {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        var (newW, newH) = (maxX - minX + 1, maxY - minY + 1);
        var newTiles = new T[newW, newH];
        //newTiles.Fill('0');
        for (int x = 0; x < newW; x++) {
            for (int y = 0; y < newH; y++) {
                var c = arr.GetOrDefault(x + minX, y + minY, emptyValue);

                newTiles[x, y] = c;
            }
        }

        offX = minX;
        offY = minY;
        return newTiles;
    }

    public static T[][] Create2DJagged<T>(int w, int h) {
        T[][] arr = new T[h][];
        for (int i = 0; i < h; i++) {
            arr[i] = new T[w];
        }

        return arr;
    }
}
