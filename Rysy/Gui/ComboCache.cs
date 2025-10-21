using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui;

public class ComboCache<T> {
    public ComboCache() {
        Token = new();
        Token.OnInvalidate += () => CachedValueDict = null;
    }

    private List<KeyValuePair<T, Searchable>>? CachedValueDict;
    private List<T>? CachedValue;
    private string? CachedSearch;
    private HashSet<string>? CachedFavorites;

    public readonly CacheToken Token;

    private NumVector2? Size;
    private int CachedSizeTextSize;

    internal NumVector2 GetSize(IEnumerable<string> values) {
        if (Settings.Instance.FontSize != CachedSizeTextSize) {
            Size = null;
        }

        //CachedSizeTextSize = Settings.Instance.FontSize;

        return Size ??= ImGuiManager.CalcListSize(values);
    }
    
    internal NumVector2 GetSize(IEnumerable<Searchable> values) {
        if (Settings.Instance.FontSize != CachedSizeTextSize) {
            Size = null;
        }

        //CachedSizeTextSize = Settings.Instance.FontSize;

        return Size ??= ImGuiManager.CalcListSize(values.Select(x => x.TextWithMods));
    }

    internal void Clear() {
        CachedValue = null;
        CachedValueDict = null;
        CachedSearch = null;
        Size = null;
    }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    internal List<KeyValuePair<T, Searchable>> GetValue(IDictionary<T, string> values, string search)
        => GetValue(values.ToDictionary(x => x.Key, x => new Searchable(x.Value)), search);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

    internal List<KeyValuePair<T, Searchable>> GetValue(IDictionary<T, Searchable> values, string search) {
        if (search != CachedSearch) {
            CachedValueDict = null;
        }

        CachedValueDict ??= values.SearchFilter(i => i.Value, search).ToList();
        CachedSearch = search;

        Token.Reset();

        return CachedValueDict;
    }

    internal List<T> GetValue(IEnumerable<T> values, Func<T, Searchable> toString, string search, HashSet<string>? favorites = null) {
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
