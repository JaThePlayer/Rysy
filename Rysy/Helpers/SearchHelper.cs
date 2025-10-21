using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Mods;
using System.Diagnostics;

namespace Rysy.Helpers;

public struct Searchable {
    public string TextWithMods { get; private init; }
        
    public string Text { get; }
        
    public IReadOnlyList<string> Mods { get; }
        
    public IReadOnlySet<string> Tags { get; }

    public Searchable(string text) : this(text, [], SearchHelper.EmptySet) {
        
    }
    
    public Searchable(string text, ModMeta? mod) : this(text, mod is {} ? [mod.Name] : [], SearchHelper.EmptySet) {
        
    }
    
    public Searchable(string text, IReadOnlyList<string> mods, IReadOnlySet<string> tags) {
        Text = text;
        Mods = mods is [] ? [ ModRegistry.VanillaMod.Name ] : mods;
        Tags = tags;
            
        if (Mods is { Count: > 0 } and not [ "Celeste" ]) {
            TextWithMods = $"{Text} [{string.Join(',', Mods.Select(ModMeta.ModNameToDisplayName))}]";
        } else {
            TextWithMods = Text;
        }
    }
    
    public static implicit operator Searchable(string text) => new(text);
    
    public static Searchable FromString(string text) => new(text);

    public override string ToString() => TextWithMods;
}

public static class SearchHelper {
    public static readonly IReadOnlySet<string> EmptySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, string> textSelector,
        string search, HashSet<string>? favorites = null)
        => source.SearchFilter(x => new Searchable(textSelector(x), [], EmptySet), search, favorites);
    
    /// <summary>
    /// Filters and orders the <paramref name="source"/> using the provided search string and list of favorites, for use with search bars in UI's.
    /// </summary>
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, Searchable> textSelector, string search, HashSet<string>? favorites = null) {
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var filter = source.Select(e => (e, Data: textSelector(e)));

        if (hasSearch) {
            /*
             * spike other - both terms need to match
             * trigger_trigger  - _ gets replaced with spaces and treated as single search term.
             * @frost_helper spike  - spikes from Frost Helper
             * @maddie|@frost spike|spinner    - from either maddie's helping hand or frost helper; either spike or spinner
             * #camera     - (triggers only) - only from "camera" tag/category
             * @vanilla    - (includes everest)
             * 
             */
            var parsed = ParseSearch(search);
            filter = filter
                .Where(e => parsed.Matches(e.Data))
                .OrderOrThenByDescending(e => parsed.StartsWith(e.Data, e.Data.Text, out _));
        }

        if (favorites is { }) {
            filter = filter.OrderOrThenByDescending(e => favorites.Contains(e.Data.Text)); // put favorites in front of other options
        }

        // order alphabetically, but don't include mod name in the ordering.
        filter = filter.OrderOrThenBy(e => e.Data.Text, new TrimModNameStringComparer());

        return filter.Select(e => e.e);
    }

    private static string ToStringUnderscoresAreSpaces(ReadOnlySpan<char> txt) {
        return txt.ToString().Replace('_', ' ').Trim();
    }
    
    internal static ISearchTerm ParseSearch(ReadOnlySpan<char> txt) {
        txt = txt.Trim();
        if (txt.IsWhiteSpace())
            return EmptySearchTerm.Instance;
        
        var termEnd = txt.IndexOfAny(" |");
        if (termEnd == -1) {
            return txt switch {
                ['@', .. var modName] => new ModSearchTerm(ToStringUnderscoresAreSpaces(modName)),
                ['#', .. var tagName] => new TagSearchTerm(ToStringUnderscoresAreSpaces(tagName)),
                _ => new TextSearchTerm(ToStringUnderscoresAreSpaces(txt))
            };
        }
        var leftSpan = txt[..termEnd];
        var rightSpan = txt[(termEnd + 1)..];
        var left = ParseSearch(leftSpan);
        var right = ParseSearch(rightSpan);

        switch (txt[termEnd]) {
            case ' ':
                return new AndSearchTerm(left, right);
            case '|':
                return new OrSearchTerm(left, right);
            default:
                throw new UnreachableException();
        }
    }
    
    internal interface ISearchTerm {
        public void RenderImGui();
        
        public bool Matches(Searchable search);
        
        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining);
    }

    private class AndSearchTerm(ISearchTerm left, ISearchTerm right) : ISearchTerm {
        public void RenderImGui() {
            left.RenderImGui();
            ImGui.SameLine(0f, 0f);
            ImGui.Text(" ");
            ImGui.SameLine(0f, 0f);
            right.RenderImGui();
        }

        public bool Matches(Searchable search) => left.Matches(search) && right.Matches(search);

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            if (!left.StartsWith(search, curr, out remaining))
                return false;

            return right.StartsWith(search, remaining, out remaining);
        }
    }
    
    private class OrSearchTerm(ISearchTerm left, ISearchTerm right) : ISearchTerm {
        public void RenderImGui() {
            left.RenderImGui();
            ImGui.SameLine(0f, 0f);
            ImGui.Text("|");
            ImGui.SameLine();
            right.RenderImGui();
        }
        
        public bool Matches(Searchable search) => left.Matches(search) || right.Matches(search);

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            return left.StartsWith(search, curr, out remaining) 
                || right.StartsWith(search, curr, out remaining);
        }
    }

    private class TextSearchTerm(string text) : ISearchTerm {
        public void RenderImGui() {
            ImGui.Text(text);
        }

        public bool Matches(Searchable search) => search.Text.Contains(text, StringComparison.OrdinalIgnoreCase);

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            if (curr.StartsWith(text, StringComparison.OrdinalIgnoreCase)) {
                remaining = curr[text.Length..];
                return true;
            }
            return false;
        }
    }
    
    private class ModSearchTerm(string modName) : ISearchTerm {
        public void RenderImGui() {
            ImGui.TextColored(Color.LightSkyBlue.ToNumVec4(), Interpolator.TempU8($"@{modName}"));
        }

        public bool Matches(Searchable search) => search.Mods.Any(x => 
            x.Contains(modName, StringComparison.OrdinalIgnoreCase)
            || (ModRegistry.GetModByName(x) is {} mod && mod.DisplayName.Contains(modName, StringComparison.OrdinalIgnoreCase)));

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            return search.Mods.Any(x => 
                x.StartsWith(modName, StringComparison.OrdinalIgnoreCase)
                || (ModRegistry.GetModByName(x) is {} mod && mod.DisplayName.StartsWith(modName, StringComparison.OrdinalIgnoreCase)));;
        }
    }
    
    private class TagSearchTerm(string tagName) : ISearchTerm {
        public void RenderImGui() {
            ImGui.TextColored(Color.Gold.ToNumVec4(), Interpolator.TempU8($"#{tagName}"));
        }
        
        public bool Matches(Searchable search) => search.Tags.Contains(tagName);

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            return true;
        }
    }
    
    private class EmptySearchTerm : ISearchTerm {
        public static readonly EmptySearchTerm Instance = new();

        public void RenderImGui() {
            
        }

        public bool Matches(Searchable search) => true;

        public bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            return true;
        }
    }

    private static IEnumerable<T> OrderOrThenBy<T, T2>(this IEnumerable<T> self, Func<T, T2> selector, IComparer<T2>? comparer = null) {
        if (self is IOrderedEnumerable<T> ordered)
            return ordered.ThenBy(selector, comparer);
        return self.OrderBy(selector, comparer);
    }

    private static IEnumerable<T> OrderOrThenByDescending<T, T2>(this IEnumerable<T> self, Func<T, T2> selector) {
        if (self is IOrderedEnumerable<T> ordered)
            return ordered.ThenByDescending(selector);
        return self.OrderByDescending(selector);
    }
    
    private struct TrimModNameStringComparer : IComparer<string> {
        public int Compare(string? x, string? y) {
            if (x is null || y is null)
                return StringComparer.Ordinal.Compare(x, y);

            return TrimModName(x).CompareTo(TrimModName(y), StringComparison.Ordinal);
        }

        private static ReadOnlySpan<char> TrimModName(ReadOnlySpan<char> from) {
            if (from is not [.., ']'])
                return from;
            
            var bracketI = from.LastIndexOf('[');
            if (bracketI >= 0) {
                return from[..bracketI];
            }

            return from;
        }
    } 
}