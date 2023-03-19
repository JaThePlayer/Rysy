using System.Collections;

namespace Rysy.Helpers;

public interface IListenableList<T> : IList<T> {
    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action? OnChanged { get; set; }
}

/// <summary>
/// Acts like a <see cref="List{T}"/>, but implements <see cref="IListenableList{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListenableList<T> : IListenableList<T> {
    private List<T> Inner;
    public Action? OnChanged { get; set; }

    public ListenableList() {
        Inner = new();
    }

    public ListenableList(Action onChanged) {
        Inner = new();

        OnChanged = onChanged;
    }

    public ListenableList(IEnumerable<T> from) {
        Inner = new(from);
    }

    public T this[int index] {
        get => Inner[index];
        set {
            Inner[index] = value;
            OnChanged?.Invoke();
        }
    }

    public int Count => Inner.Count;

    public bool IsReadOnly => false;


    public void Add(T item) {
        Inner.Add(item);
        OnChanged?.Invoke();
    }

    public void Clear() {
        Inner.Clear();
        OnChanged?.Invoke();
    }

    public bool Contains(T item) => Inner.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) {
        Inner.CopyTo(array, arrayIndex);
    }

    public int IndexOf(T item) => Inner.IndexOf(item);

    public void Insert(int index, T item) {
        Inner.Insert(index, item);
        OnChanged?.Invoke();
    }

    public bool Remove(T item) {
        if (Inner.Remove(item)) {
            OnChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) {
        Inner.RemoveAt(index);
        OnChanged?.Invoke();
    }

    public List<T>.Enumerator GetEnumerator() => Inner.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();
}
