namespace Rysy.Extensions;

public static class IListExtensions {
    public static void Swap<T>(this IList<T> list, T item, T newItem) {
        var i = list.IndexOf(item);
        if (i == -1) {
            return;
        }

        list[i] = newItem;
    }
}
