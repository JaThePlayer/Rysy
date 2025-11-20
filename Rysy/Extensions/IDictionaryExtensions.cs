namespace Rysy.Extensions;

internal static class DictionaryExtensions {
    public static TValue GetOrSetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        TKey key, Func<TKey, TValue> factory) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }
        
        var newValue = factory(key);
        dictionary[key] = newValue;
        return newValue;
    }
    
    public static TValue GetOrSetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, 
        TKey key, TValue defaultValue) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }
        
        dictionary[key] = defaultValue;
        return defaultValue;
    }

    public static int ContentsHashCode<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, 
        IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null) {
        HashCode c = new();
        foreach (var (k, v) in dictionary) {
            c.Add(k, keyComparer);
            c.Add(v, valueComparer);
        }
        return c.ToHashCode();
    }
}