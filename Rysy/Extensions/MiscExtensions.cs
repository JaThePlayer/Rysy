using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rysy.Extensions;

public static class MiscExtensions {
    public static T[] AwaitAll<T>(this IEnumerable<Task<T>> tasks) {
        var all = Task.WhenAll(tasks);
        all.Wait();

        return all.Result;
    }

    public static void AwaitAll(this IEnumerable<Task> tasks) {
        Task.WaitAll(tasks.ToArray());
    }

    public static T Abs<T>(this T num) where T : INumber<T>
        => T.Abs(num);

    public static bool Contains<T>(this T[] tiles, T value) where T : unmanaged {
        return Array.IndexOf(tiles, value) >= 0;
    }

    /// <summary>
    /// Converts this color to a <see cref="NumVector3"/>[R, G, B]
    /// </summary>
    public static NumVector3 ToNumVec3(this Color color) => color.ToVector3().ToNumerics();

    /// <summary>
    /// Converts this color to a <see cref="NumVector4"/>[R, G, B, A]
    /// </summary>
    public static NumVector4 ToNumVec4(this Color color) => color.ToVector4().ToNumerics();

    public static T[] ShallowClone<T>(this T[] array) => (T[]) array.Clone();

    public static bool Get2d(this BitArray s, int x, int y, int gridWidth) {
        var i = s.Get1dLoc(x, y, gridWidth);
        return i >= 0 && i < s.Length && s.Get(i);
    }
    
    public static void Set2d(this BitArray s, int x, int y, int gridWidth, bool value) {
        var i = s.Get1dLoc(x, y, gridWidth);
        if (i >= 0 && i < s.Length)
            s.Set(i, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point Get2dLoc(this BitArray s, int index, int gridWidth) {
        (int q, int r) = int.DivRem(index, gridWidth);
        
        return new(r, q);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Get1dLoc(this BitArray s, int x, int y, int gridWidth) {
        return x + y * gridWidth;
    }

    
    /// <summary>
    /// Returns an enumerator that enumerates through all 2d points stored in the BitArray associated with a true value.
    /// </summary>
    public static BitArray2dMatchEnumerator EnumerateTrue2dLocations(this BitArray s, int gridWidth, Point offset = default) =>
        new(s, gridWidth, offset);
}

public struct BitArray2dMatchEnumerator : IEnumerator<Point>, IEnumerable<Point> {
    private int _i;
    private int _w;
    private BitArray _arr;
    private Point _offset;
    
    public BitArray2dMatchEnumerator(BitArray s, int gridWidth, Point offset) {
        _i = -1;
        _w = gridWidth;
        _offset = offset;
        _arr = s;
    }

    public bool MoveNext() {
        var arr = _arr;
        var i = _i;
        
        while (++i < arr.Length) {
            if (arr.Get(i)) {
                _i = i;
                return true;
            }
        }
        
        _i = i;
        return false;
    }

    public void Reset() {
        _i = 0;
    }

    object IEnumerator.Current => Current;

    public Point Current => _arr.Get2dLoc(_i, _w) + _offset;

    public BitArray2dMatchEnumerator GetEnumerator() => this;
    
    public readonly void Dispose() {
        
    }

    IEnumerator<Point> IEnumerable<Point>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
