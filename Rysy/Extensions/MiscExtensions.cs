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

    extension(Color color)
    {
        /// <summary>
        /// Converts this color to a <see cref="NumVector3"/>[R, G, B]
        /// </summary>
        public NumVector3 ToNumVec3() => color.ToVector3().ToNumerics();

        /// <summary>
        /// Converts this color to a <see cref="NumVector4"/>[R, G, B, A]
        /// </summary>
        public NumVector4 ToNumVec4() => color.ToVector4().ToNumerics();
    }

    public static T[] ShallowClone<T>(this T[] array) => (T[]) array.Clone();

    extension(BitArray s)
    {
        public bool Get2d(int x, int y, int gridWidth) {
            var i = s.Get1dLoc(x, y, gridWidth);
            return i >= 0 && i < s.Length && s.Get(i);
        }

        public void Set2d(int x, int y, int gridWidth, bool value) {
            var i = s.Get1dLoc(x, y, gridWidth);
            if (i >= 0 && i < s.Length)
                s.Set(i, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point Get2dLoc(int index, int gridWidth) {
            (int q, int r) = int.DivRem(index, gridWidth);
        
            return new(r, q);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get1dLoc(int x, int y, int gridWidth) {
            return x + y * gridWidth;
        }

        /// <summary>
        /// Returns an enumerator that enumerates through all 2d points stored in the BitArray associated with a true value.
        /// </summary>
        public BitArray2dMatchEnumerator EnumerateTrue2dLocations(int gridWidth, Point offset = default) =>
            new(s, gridWidth, offset);
    }


    public static void DisposeIfDisposable(this object x) {
        if (x is IDisposable d)
            d.Dispose();
    }
}

public struct BitArray2dMatchEnumerator : IEnumerator<Point>, IEnumerable<Point> {
    private int _i;
    private int _w;
    private BitArray _arr;

    public BitArray2dMatchEnumerator(BitArray s, int gridWidth, Point offset) {
        _i = -1;
        _w = gridWidth;
        Current = offset;
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

    public Point Current => _arr.Get2dLoc(_i, _w) + field;

    public BitArray2dMatchEnumerator GetEnumerator() => this;
    
    public readonly void Dispose() {
        
    }

    IEnumerator<Point> IEnumerable<Point>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
