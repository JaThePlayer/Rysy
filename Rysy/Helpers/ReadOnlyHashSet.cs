using System.Collections;

namespace Rysy.Helpers;

/// <summary>
/// A read-only wrapper over a HashSet. Avoids boxing of the Enumerator.
/// </summary>
public readonly struct ReadOnlyHashSet<T> : IReadOnlySet<T> {
    private readonly HashSet<T> _backing;

    public ReadOnlyHashSet(HashSet<T> from) {
        _backing = from;
    }

    public HashSet<T>.Enumerator GetEnumerator() => _backing.GetEnumerator();
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _backing.Count;

    public bool Contains(T item) => _backing.Contains(item);

    public bool IsProperSubsetOf(IEnumerable<T> other) => _backing.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => _backing.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => _backing.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => _backing.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other) => _backing.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => _backing.SetEquals(other);
}