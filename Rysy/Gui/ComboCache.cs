using Rysy.Helpers;

namespace Rysy.Gui;

public class ComboCache<T> {
    public ComboCache() {
        Token = new();
        Token.OnInvalidate += () => CachedValueDict = null;
    }

    private List<KeyValuePair<T, string>>? CachedValueDict;
    private List<T>? CachedValue;
    private string? CachedSearch;
    private HashSet<string>? CachedFavorites;

    public readonly CacheToken Token;

    internal NumVector2? Size;

    internal void Clear() {
        CachedValue = null;
        CachedValueDict = null;
        CachedSearch = null;
        Size = null;
    }

    internal List<KeyValuePair<T, string>> GetValue(IDictionary<T, string> values, string search) {
        if (search != CachedSearch) {
            CachedValueDict = null;
        }

        CachedValueDict ??= values.SearchFilter(i => i.Value, search).ToList();
        CachedSearch = search;

        Token.Reset();

        return CachedValueDict;
    }

    internal List<T> GetValue(IEnumerable<T> values, Func<T, string> toString, string search, HashSet<string>? favorites = null) {
        if (search != CachedSearch || (favorites is null != CachedFavorites is null) || (CachedFavorites?.SetEquals(favorites!) ?? false)) {
            CachedValue = null;
        }

        CachedValue ??= values.SearchFilter(i => toString(i), search, favorites).ToList();
        CachedSearch = search;
        CachedFavorites = favorites;

        Token.Reset();

        return CachedValue;
    }

    private string _Search = "";
    internal string Search {
        get => _Search ?? "";
        set {
            Clear();
            _Search = value;
        }
    }
}
