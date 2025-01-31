using ImGuiNET;
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

    private NumVector2? Size;
    private int CachedSizeTextSize;

    internal NumVector2 GetSize(IEnumerable<string> values) {
        if (Settings.Instance.FontSize != CachedSizeTextSize) {
            Size = null;
        }

        //CachedSizeTextSize = Settings.Instance.FontSize;

        return Size ??= ImGuiManager.CalcListSize(values);
    }

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
