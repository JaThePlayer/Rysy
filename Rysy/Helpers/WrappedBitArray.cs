using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// Simple version of BitArray that wraps an existing array, allowing for pooling.
/// </summary>
public readonly struct WrappedBitArray {
    private const int ArrayAccessBitShift = 5;
    
    private readonly int[] _array;
    public readonly int Length;

    private WrappedBitArray(int[] backingArray) {
        _array = backingArray;
        Length = _array.Length * 32;
    }
    
    public static WrappedBitArray Rent(int minLength) {
        var arr = ArrayPool<int>.Shared.Rent(minLength / 32 + (minLength % 32 > 0 ? 1 : 0));
        Array.Clear(arr);
        return new WrappedBitArray(arr);
    }
    
    public void ReturnToPool() {
        ArrayPool<int>.Shared.Return(_array);
    }

    public BitArray ToBitArray() {
        return new(_array);
    }
    
    public bool this[int index]
    {
        get => Get(index);
        set => Set(index, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(int index)
    {
        if ((uint)index >= (uint)Length)
            ThrowArgumentOutOfRangeException(index);

        return (_array[index >> ArrayAccessBitShift] & (1 << index)) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value)
    {
        if ((uint)index >= (uint)Length)
            ThrowArgumentOutOfRangeException(index);

        int bitMask = 1 << index;
        ref int segment = ref _array[index >> ArrayAccessBitShift];

        if (value)
        {
            segment |= bitMask;
        }
        else
        {
            segment &= ~bitMask;
        }
    }

    /// <summary>
    /// Sets the given bit to `true`, returning whether there was any change.
    /// </summary>
    public bool ToggleOn(int index) {
        if ((uint)index >= (uint)Length)
            ThrowArgumentOutOfRangeException(index);
        
        ref int segment = ref _array[index >> ArrayAccessBitShift];
        var prevSegment = segment;
        segment |= 1 << index;
        
        return prevSegment != segment;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get2d(int x, int y, int gridWidth) {
        var i = Get1dLoc(x, y, gridWidth);
        return (uint)i < Length && Get(i);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set2d(int x, int y, int gridWidth, bool value) {
        var i = Get1dLoc(x, y, gridWidth);
        if ((uint)i < Length)
            Set(i, value);
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
    
    private static void ThrowArgumentOutOfRangeException(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index));
    }
    
    /// <summary>
    /// Returns an enumerator that enumerates through all 2d points stored in the BitArray associated with a true value.
    /// </summary>
    public TwoDimMatchEnumerator EnumerateTrue2dLocations(int gridWidth, Point offset = default) =>
        new(this, gridWidth, offset);
    
    public struct TwoDimMatchEnumerator : IEnumerator<Point>, IEnumerable<Point> {
        private int _i;
        private int _w;
        private WrappedBitArray _arr;
        private Point _offset;
    
        public TwoDimMatchEnumerator(WrappedBitArray s, int gridWidth, Point offset) {
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

        public TwoDimMatchEnumerator GetEnumerator() => this;
    
        public readonly void Dispose() {
        
        }

        IEnumerator<Point> IEnumerable<Point>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}