using System.Buffers;

namespace Rysy.Extensions;

public static class IListExtensions {
    public static void Swap<T>(this IList<T> list, T item, T newItem) {
        var i = list.IndexOf(item);
        if (i == -1) {
            return;
        }

        list[i] = newItem;
    }

    public static void Sort<T>(this IList<T> list, Comparison<T> comparer) {
        if (list is List<T> actualList) {
            actualList.Sort(comparer);
            return;
        }
        
        var buffer = ArrayPool<T>.Shared.Rent(list.Count);
        list.CopyTo(buffer, 0);
        buffer.AsSpan().Sort(comparer);

        for (int i = 0; i < buffer.Length; i++) {
            list[i] = buffer[i];
        }
        ArrayPool<T>.Shared.Return(buffer);
    }
}
