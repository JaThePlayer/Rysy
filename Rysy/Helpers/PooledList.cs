using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// Similar to List, but uses the Array Pool to store its items.
/// </summary>
public struct PooledList<T> : IList<T>, IDisposable {
    private T[]? _array;
    private int _count;
    
    private ArrayPool<T> Pool => ArrayPool<T>.Shared;

    public PooledList() {
        _array = [];
        _count = 0;
    }

    public PooledList(int capacity) {
        _array = [];
        _count = 0;
        EnsureCapacity(capacity);
    }

    private void EnsureCapacity(int newCapacity) {
        _array ??= [];
        
        if (_array.Length < newCapacity) {
            var pool = Pool;
            
            var newArr = pool.Rent(int.Max(newCapacity, _array.Length * 2));
            var oldArr = _array;
            oldArr.AsSpan().CopyTo(newArr);
            
            _array = newArr;

            if (oldArr.Length > 0) {
                pool.Return(oldArr, true);
            }
        }
    }
    

    public Enumerator GetEnumerator() {
        return new Enumerator(this);
    }
    
    public RefEnumerator GetRefEnumerable() {
        return new RefEnumerator(this);
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(T item) {
        EnsureCapacity(_count + 1);
        
        _array![_count] = item;
        _count++;
    }

    public void Clear() {
        _count = 0;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            _array.AsSpan().Clear();
        }
    }

    public bool Contains(T item) {
        return _array?.Contains(item) ?? false;
    }

    public void CopyTo(T[] array, int arrayIndex) {
        _array?.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item) {
        var idx = IndexOf(item);
        if (idx < 0)
            return false;

        RemoveAt(idx);
        
        return true;
    }

    public int Count => _count;
    
    public int Capacity => _array?.Length ?? 0;

    bool ICollection<T>.IsReadOnly => false;

    public int IndexOf(T item) {
        return _array is {} ? Array.IndexOf(_array, item) : -1;
    }

    public void Insert(int index, T item) {
        // Note that insertions at the end are legal.
        if ((uint)index > (uint)_count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_count);
        }
        
        if (_array is null || _count == _array.Length)
        {
            EnsureCapacity(_count + 1);
        }
        else if (index < _count)
        {
            Array.Copy(_array, index, _array, index + 1, _count - index);
        }
        
        _array![index] = item;
        _count++;
    }

    public void RemoveAt(int index) {
        if ((uint)index >= (uint)_count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count);
            return;
        }

        _array ??= [];
        
        _count--;
        if (index < _count)
        {
            Array.Copy(_array, index + 1, _array, index, _count - index);
        }
        
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _array[_count] = default!;
        }
    }

    public T this[int index] {
        get => _array![index];
        set => _array![index] = value;
    }

    public void Dispose() {
        if (_array is not { Length: > 0 } arr)
            return;
        
        _array = [];
        _count = 0;
        Pool.Return(arr);
    }
    
    public struct Enumerator(PooledList<T> backing) : IEnumerator<T> {
        private int _i = -1;
        
        public bool MoveNext() => ++_i < backing.Count;

        public void Reset() {
            _i = -1;
        }

        public T Current => backing[_i];

        object IEnumerator.Current => Current!;

        public void Dispose() {
            
        }
    }
    
    public ref struct RefEnumerator(PooledList<T> backing) {
        private int _i = -1;
        
        public bool MoveNext() => ++_i < backing.Count;

        public void Reset() {
            _i = -1;
        }

        public ref T Current => ref backing._array![_i];

        public void Dispose() {
            
        }

        public RefEnumerator GetEnumerator() => this;
    }
}