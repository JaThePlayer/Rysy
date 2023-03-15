namespace Rysy;

public static class LinqExt {
    public static IEnumerable<TTo> SelectWhereNotNull<TFrom, TTo>(this IEnumerable<TFrom> self, Func<TFrom, TTo?> cb) {
        foreach (var item in self) {
            var selected = cb(item);
            if (selected != null)
                yield return selected;
        }
    }
}
