namespace Rysy.Extensions;

internal static class DictionaryExtensions {
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        public TValue GetOrSetDefault(TKey key, Func<TKey, TValue> factory) {
            if (dictionary.TryGetValue(key, out var value)) {
                return value;
            }
        
            var newValue = factory(key);
            dictionary[key] = newValue;
            return newValue;
        }

        public TValue GetOrSetDefault(TKey key, TValue defaultValue) {
            if (dictionary.TryGetValue(key, out var value)) {
                return value;
            }
        
            dictionary[key] = defaultValue;
            return defaultValue;
        }

        public int ContentsHashCode(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null) {
            HashCode c = new();
            foreach (var (k, v) in dictionary) {
                c.Add(k, keyComparer);
                c.Add(v, valueComparer);
            }
            return c.ToHashCode();
        }
    }
}