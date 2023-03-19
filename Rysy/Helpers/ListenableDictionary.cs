using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

public class ListenableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull {
    private readonly Dictionary<TKey, TValue> Inner;

    public Action? OnChanged { get; set; }

    public ListenableDictionary(IEqualityComparer<TKey> comparer) {
        Inner = new(comparer);
    }

    public TValue this[TKey key] {
        get => Inner[key];
        set {
            Inner[key] = value;
            OnChanged?.Invoke();
        }
    }

    public ICollection<TKey> Keys => Inner.Keys;

    public ICollection<TValue> Values => Inner.Values;

    public int Count => Inner.Count;

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value) {
        Inner.Add(key, value);
        OnChanged?.Invoke();
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
        ((ICollection<KeyValuePair<TKey, TValue>>) Inner).Add(item);
        OnChanged?.Invoke();
    }

    public void Clear() {
        Inner.Clear();
        OnChanged?.Invoke();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        return Inner.Contains(item);
    }

    public bool ContainsKey(TKey key) {
        return Inner.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<TKey, TValue>>) Inner).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        return Inner.GetEnumerator();
    }

    public bool Remove(TKey key) {
        var ret = Inner.Remove(key);
        if (ret)
            OnChanged?.Invoke();
        return ret;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        var ret = ((ICollection<KeyValuePair<TKey, TValue>>) Inner).Remove(item);
        if (ret)
            OnChanged?.Invoke();
        return ret;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
        return Inner.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return Inner.GetEnumerator();
    }
}
