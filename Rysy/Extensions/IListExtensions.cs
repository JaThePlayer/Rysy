using System.Buffers;

namespace Rysy.Extensions;

public static class IListExtensions {
    extension<T>(IList<T> list)
    {
        public void Swap(T item, T newItem) {
            var i = list.IndexOf(item);
            if (i == -1) {
                return;
            }

            list[i] = newItem;
        }

        public void Sort(Comparison<T> comparer) {
            if (list is List<T> actualList) {
                actualList.Sort(comparer);
                return;
            }
        
            var buffer = ArrayPool<T>.Shared.Rent(list.Count);
            var bufferSpan = buffer.AsSpan(0, list.Count);
            list.CopyTo(buffer, 0);
            bufferSpan.Sort(comparer);

            for (int i = 0; i < bufferSpan.Length; i++) {
                list[i] = bufferSpan[i];
            }
            ArrayPool<T>.Shared.Return(buffer);
        }
    }
}
