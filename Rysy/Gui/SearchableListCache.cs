using Hexa.NET.ImGui;
using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Gui;

/// <summary>
/// ComboCache v2
/// </summary>
public sealed class SearchableListCache<T> {
    public string Search {
        get;
        set {
            if (field != value) {
                field = value;
                _filteredValues = null;
            }
        }
    } = "";
    
    public required Func<T, Searchable> ToSearchable { get; init; }

    public required IReadOnlyListenableList<T> Values {
        get;
        init {
            _currentValuesVersion = value.Version;
            FullyInvalidateCache();
            field = value;
        }
    }

    private void FullyInvalidateCache() {
        _currentValues = null;
        _filteredValues = null;
        _size = null;
    }

    private long _currentValuesVersion;
    
    /// <summary>
    /// Currently used values, unfiltered by search.
    /// </summary>
    private List<(T, Searchable Searchable)>? _currentValues;
    
    private List<(T, Searchable Searchable)>? _filteredValues;

    private NumVector2? _size;
    private int _fontSizeDuringSizeCalculation;

    public NumVector2 MaximumSize {
        get {
            var currFontSize = Settings.Instance.FontSize;
            if (_fontSizeDuringSizeCalculation != currFontSize) {
                _fontSizeDuringSizeCalculation = currFontSize;
                _size = null;
            }
            
            return _size ??= ImGuiManager.CalcListSize(
                GetUnfilteredValues()
                    .Select(x => x.Searchable.TextWithMods)
            );
        }
    }

    private List<(T, Searchable Searchable)> GetUnfilteredValues() {
        if (_currentValues is null) {
            _size = null;
            _filteredValues = null;
            _currentValues = Values.Select(x => (x, ToSearchable(x))).ToList();
        }
        
        return _currentValues;
    }

    public IReadOnlyList<(T, Searchable)> GetValues() {
        while (Values.Version != _currentValuesVersion) {
            FullyInvalidateCache();
            _currentValuesVersion = Values.Version;
        }

        var currentValues = GetUnfilteredValues();

        if (_filteredValues is null) {
            _filteredValues = currentValues.SearchFilter(x => x.Searchable, Search).ToList();
        }

        return _filteredValues;
    }

    /// <summary>
    /// Renders the contents of this list.
    /// </summary>
    /// <param name="name">Name of the list, for generating an imgui id.</param>
    /// <param name="renderMenuItem">Callback to actually render each menu item. Return true if the user has picked the element from this list.</param>
    /// <param name="newValue">Value picked from the list by the user.</param>
    /// <returns>Whether any element was picked.</returns>
    public bool RenderContents(string name, Func<T, Searchable, bool> renderMenuItem, [NotNullWhen(true)] out T? newValue) {
        var oldStyles = ImGuiManager.PopAllStyles();
        var changed = false;
        
        var search = Search;
        ImGuiManager.RenderSearchBarInDropdown(ref search);
        Search = search;

        newValue = default;

        ImGui.BeginChild($"comboInner{name}");

        var filtered = GetValues();

        foreach (var (item, searchable) in filtered) {
            if (renderMenuItem(item, searchable)) {
                newValue = item;
                changed = true;
            }
        }

        ImGui.EndChild();
            
        ImGuiManager.PushAllStyles(oldStyles);

        return changed;
    }
}
