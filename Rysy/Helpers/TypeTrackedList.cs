using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// A wrapper over <see cref="List{T}"/>, which sorts elements added to it based on their type and implemented interfaces, 
/// allowing for quick access of all elements of a given type
/// </summary>
public class TypeTrackedList<T> : IListenableList<T> {
    protected readonly List<T> Inner = [];

    private readonly Dictionary<Type, IList> _byType = [];

    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action<ListenableListChanged<T>>? OnChanged { get; set; }

    public long Version { get; private set; }

    private void CallOnChanged(ListenableListChanged<T> changed) {
        Version++;
        OnChanged?.Invoke(changed);
    }

    public T this[int index] {
        get => Inner[index];
        set {
            var prev = Inner.ElementAtOrDefault(index);
            if (prev != null) {
                UntrackItem(prev);
                CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, prev));
            }

            Inner[index] = value;
            TrackNewItem(value);
            CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Add, value));
        }
    }

    /// <summary>
    /// Quickly retrieves all elements castable to the given type.
    /// </summary>
    /// <typeparam name="TTarget">The target type, can also be an interface.</typeparam>
    /// <returns>List of all elements castable to the target type.</returns>
    public IReadOnlyList<TTarget> OfType<TTarget>() {
        if (_byType.TryGetValue(typeof(TTarget), out var list)) {
            return (IReadOnlyList<TTarget>) list;
        }

        return [];
    }
    
    /// <summary>
    /// Quickly retrieves all elements castable to the given type.
    /// </summary>
    /// <typeparam name="TTarget">The target type, can also be an interface.</typeparam>
    /// <returns>List of all elements castable to the target type.</returns>
    public IReadOnlyList<TTarget> OfType<TTarget>(Type targetType) {
        if (_byType.TryGetValue(targetType, out var list)) {
            return (IReadOnlyList<TTarget>) list;
        }

        return [];
    }
    
    /// <summary>
    /// Quickly retrieves all elements implementing the given interface type.
    /// </summary>
    /// <typeparam name="TInterface">The target type, should be an interface.</typeparam>
    /// <returns>List of all elements implementing the interface.</returns>
    public IEnumerable<(T Item, TInterface Casted)> Implementing<TInterface>() {
        var src = OfType<TInterface>();

        return src.Select(x => ((T)(object)x!, x));
    }

    public int Count => Inner.Count;

    public bool IsReadOnly => false;

    private void TrackAsType([DisallowNull] T item, Type t) {
        if (_byType.TryGetValue(t, out var l))
            l.Add(item);
        else
            _byType.Add(t, TrackerHelper.CreateListOfType(t, item));
    }

    private void TrackNewItem(T item) {
        if (item is null)
            return;

        foreach (var trackedAsType in TrackerHelper.GetTrackedAsTypes(item.GetType())) {
            TrackAsType(item, trackedAsType);
        }
    }

    private void UntrackItem(T item) {
        if (item is null)
            return;

        foreach (var trackedAsType in TrackerHelper.GetTrackedAsTypes(item.GetType())) {
            if (_byType.TryGetValue(trackedAsType, out var list)) {
                list.Remove(item);
            }
        }
    }

    public void Add(T item) {
        Inner.Add(item);

        TrackNewItem(item);

        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Add, item));
    }


    public void Clear() {
        Inner.Clear();
        _byType.Clear();

        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Refresh, default));
    }

    public bool Contains(T item) {
        return Inner.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex) {
        Inner.CopyTo(array, arrayIndex);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return Inner.GetEnumerator();
    }

    public List<T>.Enumerator GetEnumerator() {
        return Inner.GetEnumerator();
    }

    public int IndexOf(T item) {
        return Inner.IndexOf(item);
    }

    public void Insert(int index, T item) {
        Inner.Insert(index, item);

        TrackNewItem(item);

        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, item));
    }

    public bool Remove(T item) {
        UntrackItem(item);
        var ret = Inner.Remove(item);
        if (ret)
            CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, item));
        return ret;
    }

    public void RemoveAt(int index) {
        var item = Inner[index];
        UntrackItem(item);
        Inner.RemoveAt(index);

        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, item));
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return Inner.GetEnumerator();
    }
}

file static class TrackerHelper {
    private static readonly ConditionalWeakTable<Type, IReadOnlyList<Type>> Cache = [];
    private static readonly ConditionalWeakTable<Type, Type> ListTypes = [];
    
    public static IList CreateListOfType(Type type) {
        var listType = ListTypes.GetOrAdd(type, static type => typeof(List<>).MakeGenericType(type));
        return (Activator.CreateInstance(listType) as IList)!;
    }
    
    public static IList CreateListOfType(Type type, object item) {
        var list = CreateListOfType(type);
        list.Add(item);

        return list;
    }
    
    public static IReadOnlyList<Type> GetTrackedAsTypes(Type type) {
        return Cache.GetOrAdd(type, FindAllTrackedAsTypes);
    }

    private static List<Type> FindAllTrackedAsTypes(Type t) {
        List<Type> types = [ ];
        
        var nextType = t;
        while (nextType != null && nextType != typeof(object)) {
            types.Add(nextType);
            nextType = nextType.BaseType;
        }

        types.AddRange(t.GetInterfaces());

        return types;
    }
}