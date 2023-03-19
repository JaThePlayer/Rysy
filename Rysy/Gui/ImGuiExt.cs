using ImGuiNET;
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

    /// <summary>
    /// Filters and orders the <paramref name="source"/> using the provided search string and list of favorites, for use with search bars in UI's.
    /// </summary>
    public static IEnumerable<T> SearchFilter<T>(this IEnumerable<T> source, Func<T, string> textSelector, string search, HashSet<string>? favorites = null) {
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var filter = source.Select(e => (e, Name: textSelector(e)));

        if (hasSearch) {
            filter = filter
                .Where(e => e.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase)) // filter out materials that don't contain the search
                .OrderOrThenByDescending(e => e.Name.StartsWith(search, StringComparison.InvariantCultureIgnoreCase)); // put materials that start with the search first.
        }

        if (favorites is { }) {
            filter = filter.OrderOrThenByDescending(e => favorites.Contains(e.Name)); // put favorites in front of other options
        }

        filter = filter.OrderOrThenBy(e => e.Name); // order alphabetically.

        return filter.Select(e => e.e);
    }

    private static IEnumerable<T> OrderOrThenBy<T, T2>(this IEnumerable<T> self, Func<T, T2> selector) {
        if (self is IOrderedEnumerable<T> ordered)
            return ordered.ThenBy(selector);
        return self.OrderBy(selector);
    }

    private static IEnumerable<T> OrderOrThenByDescending<T, T2>(this IEnumerable<T> self, Func<T, T2> selector) {
        if (self is IOrderedEnumerable<T> ordered)
            return ordered.ThenByDescending(selector);
        return self.OrderByDescending(selector);
    }
}
