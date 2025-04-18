﻿using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using System.Linq;

namespace Rysy.Gui;

public static class ImGuiExt {

    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTooltip(this bool val, string? tooltip) {
        if (tooltip is { } && ImGui.IsItemHovered()) {
            ImGui.SetTooltip(tooltip);
        }

        return val;
    }
    
    public static bool WithTooltip(this bool val, Tooltip tooltip) {
        if (!tooltip.IsEmpty && ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            var prev = ImGuiManager.PopAllStyles();
            tooltip.RenderImGui();
            ImGuiManager.PushAllStyles(prev);
            ImGui.EndTooltip();
        }

        return val;
    }

    public static bool WithTooltip(this bool val, object? tooltip) {
        return tooltip switch {
            string s => val.WithTooltip(s),
            Tooltip s => val.WithTooltip(s),
            ITooltip s => val.WithTooltip(new Tooltip(s)),
            _ => val,
        };
    }

    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTranslatedTooltip(this bool val, string tooltipKey) {
        if (ImGui.IsItemHovered() && tooltipKey.TranslateOrNull() is { } translatedTooltip) {
            ImGui.SetTooltip(translatedTooltip);
        }

        return val;
    }
    
    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTranslatedTooltip(this bool val, Interpolator.Handler tooltipKey) {
        if (ImGui.IsItemHovered() && tooltipKey.Result.TranslateOrNull() is { } translatedTooltip) {
            ImGui.SetTooltip(translatedTooltip);
        }

        return val;
    }

    /// <summary>
    /// Filters and orders the <paramref name="source"/> using the provided search string and list of favorites, for use with search bars in UI's.
    /// </summary>
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, string> textSelector, string search, HashSet<string>? favorites = null) {
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var filter = source.Select(e => (e, Name: textSelector(e)));

        if (hasSearch) {
            var searchSplit = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            filter = filter
                .Where(e => searchSplit.All(search => e.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase))) // filter out materials that don't contain the search
                .OrderOrThenByDescending(e => e.Name.StartsWith(search, StringComparison.InvariantCultureIgnoreCase)); // put materials that start with the search first.
        }

        if (favorites is { }) {
            filter = filter.OrderOrThenByDescending(e => favorites.Contains(e.Name)); // put favorites in front of other options
        }

        // order alphabetically, but don't include mod name in the ordering.
        filter = filter.OrderOrThenBy(e => e.Name, new TrimModNameStringComparer());

        return filter.Select(e => e.e);
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
}
