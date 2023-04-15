using Rysy.Helpers;
using System;

namespace Rysy.Extensions;

public static class LinqExt {
    /// <summary>
    /// Creates an enumerable, which returns all elements of <paramref name="self"/>, but try-catches the MoveNext method and calls <paramref name="onError"/> if an exception occurs.
    /// When an exception is caught, the enumeration ends.
    /// </summary>
    public static IEnumerable<T> WithErrorCatch<T>(this IEnumerable<T> self, Action<Exception> onError) {
        using var enumerator = self.GetEnumerator();

        bool ret = true;
        while (ret) {
            try {
                ret = enumerator.MoveNext();
            } catch (Exception e) {
                onError(e);
                ret = false;
            }

            if (ret)
                yield return enumerator.Current;
        }
    }

    /// <summary>
    /// Creates an enumerable, which returns all elements of <paramref name="self"/>, but try-catches the MoveNext method and calls <paramref name="onError"/> if an exception occurs.
    /// When an exception is caught, the enumeration of <paramref name="self"/> ends, and the enumeration of the return value of <paramref name="onError"/> begins, without the try-catch
    /// </summary>
    public static IEnumerable<T> WithErrorCatch<T>(this IEnumerable<T> self, Func<Exception, IEnumerable<T>> onError) {
        using var enumerator = self.GetEnumerator();
        IEnumerable<T>? onErrorEnumerable = null;

        bool ret = true;
        while (ret) {
            try {
                ret = enumerator.MoveNext();
            } catch (Exception e) {
                onErrorEnumerable = onError(e);
                goto postError;
            }

            if (ret)
                yield return enumerator.Current;
        }

        postError:
        if (onErrorEnumerable is { }) {
            using var errorEnumerator = onErrorEnumerable.GetEnumerator();

            while (errorEnumerator.MoveNext()) {
                yield return errorEnumerator.Current;
            }
        }
    }

    /// <summary>
    /// Calls ToList on <paramref name="self"/>, unless its already of type <see cref="List{T}"/>, in which case that instance is returned without changes.
    /// </summary>
    public static List<T> ToListIfNotList<T>(this IEnumerable<T> self) {
        if (self is List<T> list)
            return list;
        return self.ToList();
    }

    public static IEnumerable<TTo> SelectWhereNotNull<TFrom, TTo>(this IEnumerable<TFrom> self, Func<TFrom, TTo?> cb) {
        foreach (var item in self) {
            var selected = cb(item);
            if (selected != null)
                yield return selected;
        }
    }

    public static IEnumerable<Task> SelectToTaskRun<T>(this IEnumerable<T> self, Action<T> action) {
        foreach (var item in self)
            yield return Task.Run(() => action(item));
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

    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> self) => self.SelectMany(e => e);
}
