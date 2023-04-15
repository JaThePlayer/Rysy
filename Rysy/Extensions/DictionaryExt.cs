namespace Rysy.Extensions;
public static class DictionaryExt {
    public static TValue? AtOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : class {
        if (dict.TryGetValue(key, out var value)) 
            return value;

        return null;
    }
}
