using System.Collections;

namespace Rysy.Helpers;

public sealed class ConcatList<T>(IReadOnlyList<T> left, IReadOnlyList<T> right) : IReadOnlyList<T> {
    private readonly IEnumerable<T> _enumerable = left.Concat(right);
    
    public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => left.Count + right.Count;

    public T this[int index] => index < left.Count ? left[index] : right[index - left.Count];
}