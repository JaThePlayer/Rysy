using System.Collections;

namespace Rysy.Helpers;

/// <summary>
/// A read-only wrapper over an array. Avoids boxing of the Enumerator.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ReadOnlyArray<T>(T[] from) : IReadOnlyList<T> {
    public Enumerator GetEnumerator() => new(from);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public int Count => from.Length;

    public T this[int index] => from[index];

    public T GetAtOr(int index, T def) {
        var arr = from;
        return (uint) index <= arr.Length ? arr[index] : def;
    }

    public struct Enumerator(T[] backing) : IEnumerator<T> {
        private int _i = -1;
        
        public bool MoveNext() => ++_i < backing.Length;

        public void Reset() {
            _i = -1;
        }

        public T Current => backing[_i];

        object IEnumerator.Current => Current!;

        public void Dispose() {
            
        }
    }
}

/// <summary>
/// A read-only wrapper over a list, used to avoid enumerator allocations from IReadOnlyList
/// </summary>
public readonly struct ReadOnlyList<T>(List<T> from) : IReadOnlyList<T> {
    public List<T>.Enumerator GetEnumerator() => from.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => from.Count;
    

    public T this[int index] => from[index];
    
    public static implicit operator ReadOnlyList<T>(List<T> from) => new(from);
}