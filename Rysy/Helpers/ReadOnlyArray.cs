using System.Collections;

namespace Rysy.Helpers;

/// <summary>
/// A read-only wrapper over a array. Avoids boxing of the Enumerator.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ReadOnlyArray<T> : IReadOnlyList<T> {
    private readonly T[] _backing;

    public ReadOnlyArray(T[] from) {
        _backing = from;
    }

    public Enumerator GetEnumerator() => new(_backing);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public int Count => _backing.Length;

    public T this[int index] => _backing[index];

    public struct Enumerator(T[] backing) : IEnumerator<T> {
        private int _i = -1;
        
        public bool MoveNext() => ++_i < backing.Length;

        public void Reset() {
            _i = -1;
        }

        public T Current => backing[_i];

        object IEnumerator.Current => Current;

        public void Dispose() {
            
        }
    }
}