using Rysy.Helpers;

namespace Rysy.Gui;

public class ComboCache<T> {
    public ComboCache() {
        Token = new();
        Token.OnInvalidate += () => CachedValue = null;
    }

    private List<KeyValuePair<T, string>>? CachedValue;
    private string? CachedSearch;

    public readonly CacheToken Token;

    internal NumVector2? Size;

    internal List<KeyValuePair<T, string>> GetValue(IDictionary<T, string> values, string search) {
        if (search != CachedSearch) {
            CachedValue = null;
        }

        CachedValue ??= values.SearchFilter(i => i.Value, search).ToList();
        CachedSearch = search;

        Token.Reset();

        return CachedValue;
    }
}
