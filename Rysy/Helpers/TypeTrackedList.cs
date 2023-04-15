using System.Collections;

namespace Rysy.Helpers;

/// <summary>
/// A wrapper over <see cref="List{T}"/>, which sorts elements added to it based on their type and implemented interfaces, 
/// allowing for quick access of all elements of a given type
/// </summary>
public class TypeTrackedList<T> : IListenableList<T> {
    protected List<T> Inner = new();

    private Dictionary<Type, List<T>> ByType = new();

    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action? OnChanged { get; set; }

    public T this[int index] {
        get => Inner[index];
        set {
            var prev = Inner.ElementAtOrDefault(index);
            if (prev != null)
                UntrackItem(prev);

            Inner[index] = value;
            TrackNewItem(value);

            OnChanged?.Invoke();
        }
    }

    public List<T> this[Type type] {
        get => ByType.GetValueOrDefault(type) ?? (ByType[type] = new());
    }

    public int Count => Inner.Count;

    public bool IsReadOnly => false;

    private void TrackAsType(T item, Type t) {
        if (ByType.TryGetValue(t, out var l))
            l.Add(item);
        else
            ByType.Add(t, new() { item });
    }

    private void TrackNewItem(T item) {
        var t = item!.GetType();

        TrackAsType(item, t);
        foreach (var inter in t.GetInterfaces())
            TrackAsType(item, inter);
    }

    private void UntrackItem(T item) {
        var t = item!.GetType();

        ByType[t].Remove(item);
        foreach (var inter in t.GetInterfaces())
            ByType[inter].Remove(item);
    }

    public void Add(T item) {
        Inner.Add(item);

        TrackNewItem(item);

        OnChanged?.Invoke();
    }


    public void Clear() {
        Inner.Clear();
        ByType.Clear();

        OnChanged?.Invoke();
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

        OnChanged?.Invoke();
    }

    public bool Remove(T item) {
        UntrackItem(item);
        var ret = Inner.Remove(item);
        if (ret)
            OnChanged?.Invoke();
        return ret;
    }

    public void RemoveAt(int index) {
        UntrackItem(Inner[index]);
        Inner.RemoveAt(index);

        OnChanged?.Invoke();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return Inner.GetEnumerator();
    }
}
