using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Mods;
using System.Diagnostics;

namespace Rysy.Helpers;

public struct Searchable {
    public string TextWithMods { get; private init; }
        
    public string Text { get; }
        
    public IReadOnlyList<string> Mods { get; }
        
    public IReadOnlyList<string> Tags { get; }

    public Searchable(string text) : this(text, [], []) {
        
    }
    
    public Searchable(string text, ModMeta? mod) : this(text, mod is {} ? [mod.Name] : [], []) {
        
    }
    
    public Searchable(string text, IReadOnlyList<string> mods, IReadOnlyList<string>? tags) {
        Text = text;
        Mods = mods is [] ? [ ModRegistry.VanillaMod.Name ] : mods;
        Tags = tags ?? [];
            
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
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, string> textSelector,
        string search, HashSet<string>? favorites = null)
        => source.SearchFilter(x => new Searchable(textSelector(x), [], []), search, favorites);
    
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

    private static ReadOnlySpan<char> TrimLeft(ReadOnlySpan<char> txt, out string trimmed) {
        var ret = txt.TrimStart();
        if (txt.Length == ret.Length) {
            trimmed = string.Empty;
            return ret;
        }
        
        trimmed = txt[..^ret.Length].ToString();
        
        return ret;
    }
    
    private static ReadOnlySpan<char> TrimRight(ReadOnlySpan<char> txt, out string trimmed) {
        var ret = txt.TrimEnd();
        if (txt.Length == ret.Length) {
            trimmed = string.Empty;
            return ret;
        }
        
        trimmed = txt.Slice(txt.Length - (txt.Length - ret.Length)).ToString();
        return ret;
    }
    
    internal static SearchTerm ParseSearch(ReadOnlySpan<char> txt) {
        txt = TrimLeft(txt, out var leftTrivia);
        txt = TrimRight(txt, out var rightTrivia);
        if (txt.IsWhiteSpace())
            return new EmptySearchTerm(leftTrivia, rightTrivia);

        SearchTerm term;
        
        var termEnd = txt.IndexOfAny(" |");
        if (termEnd == -1) {
            term = txt switch {
                ['@', .. var modName] => new ModSearchTerm(modName),
                ['#', .. var tagName] => new TagSearchTerm(tagName),
                _ => new TextSearchTerm(txt)
            };
        } else {
            var leftSpan = txt[..termEnd];
            var rightSpan = txt[(termEnd + 1)..];
            var left = ParseSearch(leftSpan);
            var right = ParseSearch(rightSpan);

            term = txt[termEnd] switch {
                ' ' => new AndSearchTerm(left, right),
                '|' => new OrSearchTerm(left, right),
                _ => throw new UnreachableException()
            };
        }
        
        term.LeftTrivia = leftTrivia;
        term.RightTrivia = rightTrivia;
            
        return term;
    }
    
    internal abstract class SearchTerm {
        public string LeftTrivia { get; set; }
        public string RightTrivia { get; set; }
        
        protected abstract void RenderImGuiInner();

        public void RenderImGui() {
            if (LeftTrivia is not "") {
                ImGui.Text(LeftTrivia);
                ImGui.SameLine(0f, 0f);
            }

            RenderImGuiInner();
            
            if (RightTrivia is not "") {
                ImGui.SameLine(0f, 0f);
                ImGui.Text(RightTrivia);
            }
        }
        
        public abstract bool Matches(Searchable search);
        
        public abstract bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining);
    }

    private sealed class AndSearchTerm(SearchTerm left, SearchTerm right) : SearchTerm {
        protected override void RenderImGuiInner() {
            left.RenderImGui();
            ImGui.SameLine(0f, 0f);
            ImGui.Text(" ");
            ImGui.SameLine(0f, 0f);
            right.RenderImGui();
        }

        public override bool Matches(Searchable search) => left.Matches(search) && right.Matches(search);

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            if (!left.StartsWith(search, curr, out remaining))
                return false;

            return right.StartsWith(search, remaining, out remaining);
        }
    }
    
    private sealed class OrSearchTerm(SearchTerm left, SearchTerm right) : SearchTerm {
        protected override void RenderImGuiInner() {
            left.RenderImGui();
            ImGui.SameLine(0f, 0f);
            ImGui.Text("|");
            ImGui.SameLine(0f, 0f);
            right.RenderImGui();
        }
        
        public override bool Matches(Searchable search) => left.Matches(search) || right.Matches(search);

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            return left.StartsWith(search, curr, out remaining) 
                || right.StartsWith(search, curr, out remaining);
        }
    }

    private sealed class TextSearchTerm(ReadOnlySpan<char> txt) : SearchTerm {
        private readonly string _term = ToStringUnderscoresAreSpaces(txt);
        private readonly byte[] _termU8 = Interpolator.TempU8(txt).ToArray();
        
        protected override void RenderImGuiInner() {
            ImGui.Text(_termU8);
        }

        public override bool Matches(Searchable search) => search.Text.Contains(_term, StringComparison.OrdinalIgnoreCase);

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            if (curr.StartsWith(_term, StringComparison.OrdinalIgnoreCase)) {
                remaining = curr[_term.Length..];
                return true;
            }
            return false;
        }
    }
    
    private sealed class ModSearchTerm(ReadOnlySpan<char> txt) : SearchTerm {
        private readonly string _modName = ToStringUnderscoresAreSpaces(txt);
        private readonly byte[] _txtU8 = Interpolator.TempU8($"@{txt}").ToArray();
        
        protected override void RenderImGuiInner() {
            ImGui.TextColored(ImGuiThemer.Current.ImGuiStyle.ModNameColor.ToNumVec4(), _txtU8);
        }

        public override bool Matches(Searchable search) => search.Mods.Any(x => 
            x.Contains(_modName, StringComparison.OrdinalIgnoreCase)
            || (ModRegistry.GetModByName(x) is {} mod && mod.DisplayName.Contains(_modName, StringComparison.OrdinalIgnoreCase)));

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            return search.Mods.Any(x => 
                x.StartsWith(_modName, StringComparison.OrdinalIgnoreCase)
                || (ModRegistry.GetModByName(x) is {} mod && mod.DisplayName.StartsWith(_modName, StringComparison.OrdinalIgnoreCase)));;
        }
    }
    
    private sealed class TagSearchTerm(ReadOnlySpan<char> txt) : SearchTerm {
        private readonly string _tagName = ToStringUnderscoresAreSpaces(txt);
        private readonly byte[] _txtU8 = Interpolator.TempU8(txt).ToArray();
        
        protected override void RenderImGuiInner() {
            ImGui.TextColored(ImGuiThemer.Current.ImGuiStyle.TagColor.ToNumVec4(), Interpolator.TempU8($"#{_txtU8}"));
        }
        
        public override bool Matches(Searchable search) => search.Tags.Any(x => x.Contains(_tagName, StringComparison.OrdinalIgnoreCase));

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
            remaining = curr;
            return search.Tags.Any(x => x.StartsWith(_tagName, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    private sealed class EmptySearchTerm : SearchTerm {
        public EmptySearchTerm(string leftTrivia, string rightTrivia) {
            LeftTrivia = leftTrivia;
            RightTrivia = rightTrivia;
        }
        
        protected override void RenderImGuiInner() {
            
        }

        public override bool Matches(Searchable search) => true;

        public override bool StartsWith(Searchable search, ReadOnlySpan<char> curr, out ReadOnlySpan<char> remaining) {
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