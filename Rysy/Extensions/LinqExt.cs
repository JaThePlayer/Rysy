namespace Rysy;

public static class LinqExt {
    public static IEnumerable<TTo> SelectWhereNotNull<TFrom, TTo>(this IEnumerable<TFrom> self, Func<TFrom, TTo?> cb) {
        foreach (var item in self) {
            var selected = cb(item);
            if (selected != null)
                yield return selected;
        }
    }

    public static IEnumerable<Task> SelectToTaskRun<T>(this IEnumerable<T> self, Action<T> action) {
        foreach (var item in self) {
            yield return Task.Run(() => action(item));
        }
    }

    public static IEnumerable<T> Do<T>(this IEnumerable<T> self, Action action) {
        action();
        foreach (var item in self) {
            yield return item;
        }
    }

    public static IEnumerable<T> Apply<T>(this IEnumerable<T> self, Action<T> action) {
        foreach (var item in self) {
            action(item);
            yield return item;
        }
    }

    public static ListenableList<T> ToListenableList<T>(this IEnumerable<T> self) {
        return new(self);
    }

    public static ListenableList<T> ToListenableList<T>(this IEnumerable<T> self, Action onChange) {
        return new(self) {
            OnChanged = onChange,
        };
    }
}
