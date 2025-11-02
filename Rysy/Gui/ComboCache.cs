using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui;

public class ComboCache<T> {
    public ComboCache() {
        Token = new();
        Token.OnInvalidate += () => _cachedValueDict = null;
    }

    private List<KeyValuePair<T, Searchable>>? _cachedValueDict;
    private List<(T, Searchable)>? _cachedValue;
    private string? _cachedSearch;

    public readonly CacheToken Token;

    private NumVector2? _size;
    private int _cachedSizeTextSize;

    internal NumVector2 GetSize(IEnumerable<string> values) {
        if (Settings.Instance.FontSize != _cachedSizeTextSize) {
            _size = null;
        }

        _cachedSizeTextSize = Settings.Instance.FontSize;

        return _size ??= ImGuiManager.CalcListSize(values);
    }
    
    internal NumVector2 GetSize(IEnumerable<Searchable> values) {
        if (Settings.Instance.FontSize != _cachedSizeTextSize) {
            _size = null;
        }

        //CachedSizeTextSize = Settings.Instance.FontSize;

        return _size ??= ImGuiManager.CalcListSize(values.Select(x => x.TextWithMods));
    }

    internal void Clear() {
        _cachedValue = null;
        _cachedValueDict = null;
        _cachedSearch = null;
        _size = null;
    }

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    internal List<KeyValuePair<T, Searchable>> GetValue(IDictionary<T, string> values, string search)
        => GetValue(values.ToDictionary(x => x.Key, x => new Searchable(x.Value)), search);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

    internal List<KeyValuePair<T, Searchable>> GetValue(IDictionary<T, Searchable> values, string search) {
        if (search != _cachedSearch) {
            _cachedValueDict = null;
        }

        _cachedValueDict ??= values.SearchFilter(i => i.Value, search).ToList();
        _cachedSearch = search;

        Token.Reset();

        return _cachedValueDict;
    }
    
    internal List<(T, Searchable)> GetValue(IEnumerable<T> values, Func<T, Searchable> toString, string search) {
        if (search != _cachedSearch) {
            _cachedValue = null;
        }

        _cachedValue ??= values.SearchFilterWithSearchable(toString, search).ToList();
        _cachedSearch = search;

        Token.Reset();

        return _cachedValue;
    }

    private string _search = "";
    internal string Search {
        get => _search ?? "";
        set {
            Clear();
            _search = value;
        }
    }
}
