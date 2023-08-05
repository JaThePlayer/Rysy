using Rysy.Helpers;
using System.Collections;
using System.Collections.Generic;

namespace Rysy.Extensions;

public static class LinqExt {

    /// <summary>
    /// Returns the element at <paramref name="self"/>[<paramref name="index"/>], or <paramref name="def"/> if the list doesn't contain an element at that index.
    /// </summary>
    public static T AtOrDefault<T>(this IList<T> self, int index, T def) {
        if (index < self.Count)
            return self[index];

        return def;
    }

    public static Dictionary<TKey, TValue> CreateMerged<TKey, TValue>(this Dictionary<TKey, TValue> self, Dictionary<TKey, TValue> other)
        where TKey : notnull {
        var merged = new Dictionary<TKey, TValue>(self);
        foreach (var (key, val) in other) {
            merged[key] = val;
        }

        return merged;
    }


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

    public static IEnumerable<TTo> SelectWhereNotNull<TFrom, TTo>(this IEnumerable<TFrom> self, Func<TFrom, TTo?> cb)
        where TTo : struct {
        foreach (var item in self) {
            var selected = cb(item);
            if (selected != null)
                yield return selected.Value;
        }
    }

    public static IEnumerable<TTo> SelectWhereNotNull<TFrom, TTo>(this IEnumerable<TFrom> self, Func<TFrom, TTo?> cb)
    where TTo : class {
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

    public static Dictionary<TKey, TValue> SafeToDictionary<TIn, TKey, TValue>(this IEnumerable<TIn> values, Func<TIn, TKey> keyGetter, Func<TIn, TValue> valueGetter)
        where TKey : notnull {
        var dict = new Dictionary<TKey, TValue>();

        foreach (var value in values) {
            var key = keyGetter(value);
            //if (dict.ContainsKey(key)) {
            //    ("duplicate:", value, key, valueGetter(value)).LogAsJson();
            //}

            dict[keyGetter(value)] = valueGetter(value);
        }

        return dict;
    }

    public static Dictionary<TKey, TValue> SafeToDictionary<TIn, TKey, TValue>(this IEnumerable<TIn> values, Func<TIn, (TKey, TValue)> converter)
    where TKey : notnull {
        var dict = new Dictionary<TKey, TValue>();

        foreach (var entry in values) {
            var (key, val) = converter(entry);
            //if (dict.ContainsKey(key)) {
            //    ("duplicate:", entry, key, val).LogAsJson();
            //}

            dict[key] = val;
        }

        return dict;
    }

    public static ListenableList<T> ToListenableList<T>(this IEnumerable<T> self) {
        return new(self);
    }

    public static ListenableList<T> ToListenableList<T>(this IEnumerable<T> self, Action onChange) {
        return new(self) {
            OnChanged = onChange,
        };
    }

    public static PlacementList ToPlacementList(this IEnumerable<Placement> self) {
        return new(self);
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        where TKey : notnull
        => new(keyValuePairs);

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs, IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
        => new(keyValuePairs, comparer);

    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> self) => self.SelectMany(e => e);

    public static IEnumerable<TOut> SelectTuple<T1, T2, TOut>(this IEnumerable<(T1, T2)> self, Func<T1, T2, TOut> callback) {
        foreach (var item in self) {
            yield return callback(item.Item1, item.Item2);
        }
    }

    public static IEnumerable<TOut> SelectTuple<T1, T2, T3, TOut>(this IEnumerable<(T1, T2, T3)> self, Func<T1, T2, T3, TOut> callback) {
        foreach (var item in self) {
            yield return callback(item.Item1, item.Item2, item.Item3);
        }
    }

    /// <summary>
    /// Converts <paramref name="self"/> to an enumerator which returns this object as the only item.
    /// </summary>
    public static SingleEnumerator<T> ToSelfEnumerator<T>(this T self) => new(self);

    public struct SingleEnumerator<T> : IEnumerator<T> {
        private bool _moved = false;
        private T Item;

        internal SingleEnumerator(T item) {
            Item = item;
        }

        public T Current => Item;

        object IEnumerator.Current => Item!;

        public void Dispose() {
        }

        public bool MoveNext() {
            if (!_moved) {
                _moved = true;
                return true;
            }
            return false;
        }

        public void Reset() {
        }
    }
}
