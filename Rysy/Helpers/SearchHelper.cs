using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Mods;
using System.Diagnostics;

namespace Rysy.Helpers;

public class Searchable {
    public bool IsFavourite { get; set; }

    public string TextWithMods { get; private init; }

    public string Text { get; }

    public IReadOnlyList<string> Mods { get; }
    
    public string? DefiningMod { get; }
        
    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> AlternativeNames { get; init; } = [];

    private bool HasNonVanillaMods => Mods is { Count: > 0 } and not ["Celeste"];

    public Searchable(string text) : this(text, [], []) {
        
    }
    
    public Searchable(string text, ModMeta? mod) : this(text, mod is {} ? [mod.Name] : [], []) {
        
    }

    public Searchable(string text, IReadOnlyList<string> mods, IReadOnlyList<string>? tags, string? definingMod = null) {
        Text = text;
        DefiningMod = definingMod;
        Mods = mods is [] ? [ ModRegistry.VanillaMod.Name ] : mods;
        Tags = tags ?? [];
            
        if (HasNonVanillaMods) {
            TextWithMods = $"{Text} [{string.Join(',', Mods.Select(ModMeta.ModNameToDisplayName))}]";
        } else {
            TextWithMods = Text;
        }
    }
    
    public bool AreAssociatedModsADependencyOfMod(ModMeta? mod = null) {
        mod ??= EditorState.Map?.Mod;
        if (mod is null) {
            return true;
        }

        foreach (var associated in Mods) {
            if (!mod.DependencyMet(associated)) {
                return false;
            }
        }
        
        return true;
    }

    public override string ToString() => TextWithMods;

    private void BeforeName(bool depsMet) {
        if (!depsMet)
            ImGuiManager.PushNullStyle();
    }
    
    private void AfterName(bool depsMet) {
        if (!depsMet)
            ImGuiManager.PopNullStyle();
    }
    
    public void RenderImGuiText(ModMeta? currentMod = null) {
        var depsMet = AreAssociatedModsADependencyOfMod(currentMod);
        BeforeName(depsMet);
        
        if (IsFavourite) {
            ImGui.Text(ImGuiManager.PerFrameInterpolator.Utf8($"* {TextWithMods}"));
        } else {
            ImGui.Text(TextWithMods);
        }

        AfterName(depsMet);
    }
    
    public bool RenderImGuiMenuItem(ModMeta? currentMod = null) {
        var depsMet = AreAssociatedModsADependencyOfMod(currentMod);
        BeforeName(depsMet);
        
        bool ret = IsFavourite
            ? ImGui.MenuItem(ImGuiManager.PerFrameInterpolator.Utf8($"{TextWithMods.ToImguiEscaped()}"))
            : ImGui.MenuItem(TextWithMods.ToImguiEscaped());

        AfterName(depsMet);
        
        return ret;
    }
    
    public void RenderImGuiInfo(ModMeta? currentMod = null) {
        RenderModList(currentMod);
        RenderTagList(Tags);
        RenderAlternativeNamesList();
    }

    private void RenderModList(ModMeta? currentMod) {
        if (HasNonVanillaMods) {
            var associated = Mods;
            currentMod ??= EditorState.Map?.Mod;
            
            ImGuiManager.TranslatedText("rysy.search.associatedMods");

            ImGui.SameLine();
            var wrapX = ImGui.GetCursorPosX();
            foreach (var (mod, isLast) in associated.CheckedIfLast()) {
                var displayName = ModMeta.ModNameToDisplayName(mod);
                if (!currentMod?.DependencyMet(mod) ?? false) {
                    ImGuiManager.PushInvalidStyle();
                    ImGuiManager.RenderTextWrapped(displayName, wrapX);
                    ImGuiManager.PopInvalidStyle();
                } else {
                    ImGuiManager.RenderTextWrapped(displayName, wrapX);
                }

                if (!isLast) {
                    ImGui.SameLine(0f, 0f);
                    ImGui.Text(",");
                    ImGui.SameLine();
                }
            }

            if (DefiningMod is { } defining && (associated.Count != 1 || associated[0] != defining)) {
                ImGui.BeginDisabled();
                ImGuiManager.TranslatedText("rysy.search.definedBy");
                ImGui.SameLine();
                ImGui.TextWrapped(ModMeta.ModNameToDisplayName(defining));
                ImGui.EndDisabled();
            }
        }
    }
    
    private void RenderAlternativeNamesList() {
        if (AlternativeNames.Count <= 0)
            return;
        
        ImGui.BeginDisabled();
        ImGuiManager.TranslatedText("rysy.search.alternativeNames");
        ImGui.SameLine();
        var wrapX = ImGui.GetCursorPosX();
        foreach (var (altName, isLast) in AlternativeNames.CheckedIfLast()) {
            ImGui.SameLine();
            ImGuiManager.RenderTextWrapped(
                isLast ? Interpolator.TempU8(altName) : Interpolator.TempU8($"{altName},"),
                wrapX
            );
        }
        ImGui.EndDisabled();
    }

    public static void RenderTagList(IReadOnlyList<string> tags) {
        if (tags.Count > 0) {
            ImGuiManager.TranslatedText("rysy.search.tags");
            ImGui.SameLine(); 
            var wrapX = ImGui.GetCursorPosX();
            foreach (var (tag, isLast) in tags.CheckedIfLast()) {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Themes.Current.ImGuiStyle.TagColor.ToNumVec4());
                ImGuiManager.RenderTextWrapped(
                    isLast ? Interpolator.TempU8($"#{tag}") : Interpolator.TempU8($"#{tag},"),
                    wrapX
                );
                ImGui.PopStyleColor(1);
            }
        }
    }
}

public static class SearchHelper {
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, string> textSelector,
        string search)
        => source.SearchFilter(x => new Searchable(textSelector(x), [], []), search);

    /// <summary>
    /// Filters and orders the <paramref name="source"/> using the provided search string and list of favorites, for use with search bars in UI's.
    /// </summary>
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, Searchable> textSelector, string search)
        => source.SearchFilterWithSearchable(textSelector, search).Select(e => e.Item1);
    
    /// <summary>
    /// Filters and orders the <paramref name="source"/> using the provided search string and list of favorites, for use with search bars in UI's.
    /// </summary>
    public static IEnumerable<(T, Searchable)> SearchFilterWithSearchable<T>(this IEnumerable<T> source, Func<T, Searchable> textSelector, string search) {
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
                .OrderOrThenBy(e => e.Data.AlternativeNames.Any(x => parsed.StartsWith(e.Data, x, out _)))
                .OrderOrThenByDescending(e => parsed.StartsWith(e.Data, e.Data.Text, out _));
        }

        filter = filter.OrderOrThenByDescending(e => e.Data.IsFavourite); // put favorites in front of other options

        // order alphabetically, but don't include mod name in the ordering.
        filter = filter.OrderOrThenBy(e => e.Data.Text, new TrimModNameStringComparer());

        return filter;
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
        
        trimmed = txt[^(txt.Length - ret.Length)..].ToString();
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

        public override bool Matches(Searchable search) {
            if (search.Text.Contains(_term, StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var altName in search.AlternativeNames) {
                if (altName.Contains(_term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

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
            ImGui.TextColored(Themes.Current.ImGuiStyle.ModNameColor.ToNumVec4(), _txtU8);
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
            ImGui.TextColored(Themes.Current.ImGuiStyle.TagColor.ToNumVec4(), Interpolator.TempU8($"#{_txtU8}"));
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