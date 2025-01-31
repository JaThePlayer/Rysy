using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

/// <summary>
/// Stores a reference to an element in a ListenableDictionary.
/// Whenever that dictionary gets edited, the value stored in this reference will be lazily updated.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ListenableDictionaryRef<TKey, TValue> where TKey : notnull {
    private long Version { get; set; }
    public TKey Key { get; private init; }
    
    private TValue? _value;
    private bool _valid;
    
    /// <returns>Whether the value got changed</returns>
    private bool UpdateValue() {
        var dictVer = _dict.Version;
        if (dictVer == Version) 
            return false;
        
        Version = dictVer;
        _valid = true;

        if (_dict.TryGetValue(Key, out _value)) 
            return true;
        
        _value = default;
        _valid = false;

        return true;

    }
    
    public TValue? Value {
        get {
            UpdateValue();

            if (!_valid)
                throw new KeyNotFoundException(Key.ToString());
            
            return _value;
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out TValue? value, out bool changed) {
        changed = UpdateValue();

        if (!_valid) {
            value = default;
            return false;
        }

        value = _value!;
        return true;
    }

    private readonly ReadOnlyListenableDictionary<TKey, TValue> _dict;

    internal ListenableDictionaryRef(ReadOnlyListenableDictionary<TKey, TValue> dict, TKey key) {
        Version = long.MinValue;
        _dict = dict;
        Key = key;
    }
}

public class ListenableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull {
    internal readonly Dictionary<TKey, TValue> _inner;

    public Action? OnChanged { get; set; }

    /// <summary>
    /// Keeps track of how many times the dictionary got mutated
    /// </summary>
    public long Version { get; private set; }
    
    public ListenableDictionary(IEqualityComparer<TKey> comparer) {
        _inner = new(comparer);
    }
    
    public ListenableDictionary() {
        _inner = new();
    }

    public static implicit operator ReadOnlyListenableDictionary<TKey, TValue>(ListenableDictionary<TKey, TValue> d) => new(d);

    public ReadOnlyListenableDictionary<TKey, TValue> AsReadonly() => new(this);
    
    public Cache<T> CreateCache<T>(Func<ReadOnlyListenableDictionary<TKey, TValue>, T> generator) where T : class {
        return AsReadonly().CreateCache(generator);
    }
    
    private void HandleOnChanged() {
        Version++;
        OnChanged?.Invoke();
    }
    
    public TValue this[TKey key] {
        get => _inner[key];
        set {
            _inner[key] = value;
            HandleOnChanged();
        }
    }
    
    /// <summary>
    /// Creates a reference to the value stored at the given key. This reference will get lazily updated with the
    /// new value each time the dictionary is mutated, without subscribing to any events.
    /// </summary>
    public ListenableDictionaryRef<TKey, TValue> GetReference(TKey key)
        => new(this, key);

    public ICollection<TKey> Keys => _inner.Keys;

    public ICollection<TValue> Values => _inner.Values;

    public int Count => _inner.Count;

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value) {
        _inner.Add(key, value);
        HandleOnChanged();
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
        ((ICollection<KeyValuePair<TKey, TValue>>) _inner).Add(item);
        HandleOnChanged();
    }

    public void Clear() {
        _inner.Clear();
        HandleOnChanged();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        return _inner.Contains(item);
    }

    public bool ContainsKey(TKey key) {
        return _inner.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<TKey, TValue>>) _inner).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        return _inner.GetEnumerator();
    }

    public bool Remove(TKey key) {
        var ret = _inner.Remove(key);
        if (ret)
            HandleOnChanged();
        return ret;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        var ret = ((ICollection<KeyValuePair<TKey, TValue>>) _inner).Remove(item);
        if (ret)
            HandleOnChanged();
        return ret;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
        return _inner.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return _inner.GetEnumerator();
    }

    /*
     Not available due to Rider bug breaking hot reload :/
    public Dictionary<TKey, TValue>.AlternateLookup<T> GetAlternateLookup<T>()
        where T : notnull, allows ref struct {
        return _inner.GetAlternateLookup<T>();
    }
    */

    public TValue? GetValueOrDefault(TKey key) {
        return _inner.GetValueOrDefault(key);
    }
}

// Needed because GetAlternateLookup cannot work on a generic due to Rider bug breaking hot reload :/
internal static class ListenableDictionaryExtensions {
    public static Dictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> GetSpanAlternateLookup<TValue>(this ListenableDictionary<string, TValue> dict) {
        return dict._inner.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}

public readonly struct ReadOnlyListenableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull {
    private readonly ListenableDictionary<TKey, TValue> _inner;

    public ReadOnlyListenableDictionary(ListenableDictionary<TKey, TValue> d) {
        _inner = d;
    }

    public readonly Action? OnChanged {
        get => _inner.OnChanged;
        set => _inner.OnChanged = value;
    }

    public long Version => _inner.Version;

    public Cache<T> CreateCache<T>(Func<ReadOnlyListenableDictionary<TKey, TValue>, T> generator) where T : class {
        var inner = _inner;
        var token = new CacheToken();
        var cache = new Cache<T>(token, () => generator(inner));
        OnChanged += token.InvalidateThenReset;

        return cache;
    }

    public ListenableDictionaryRef<TKey, TValue> GetReference(TKey key)
        => new(this, key);

    public readonly TValue this[TKey key] => _inner[key];

    public readonly IEnumerable<TKey> Keys => _inner.Keys;

    public readonly IEnumerable<TValue> Values => _inner.Values;

    public readonly int Count => _inner.Count;

    public readonly bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    public readonly IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();

    public readonly bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryGetValue(key, out value);

    readonly IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}
