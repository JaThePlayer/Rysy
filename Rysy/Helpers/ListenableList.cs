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

    private bool Suppressed;

    public void SuppressCallbacks() {
        Suppressed = true;
    }

    public void Unsuppress() {
        Suppressed = false;
    }
    
    private void CallOnChanged() {
        if (!Suppressed)
            OnChanged?.Invoke();
    }

    public ListenableList() {
        Inner = new();
    }

    public ListenableList(Action onChanged) {
        Inner = new();

        OnChanged = onChanged;
    }

    public ListenableList(Action onChanged, int capacity) {
        Inner = new(capacity);

        OnChanged = onChanged;
    }

    public ListenableList(int capacity) {
        Inner = new(capacity);
    }

    public ListenableList(IEnumerable<T> from) {
        Inner = new(from);
    }

    public T this[int index] {
        get => Inner[index];
        set {
            Inner[index] = value;
            CallOnChanged();
        }
    }

    public int Count => Inner.Count;

    public bool IsReadOnly => false;


    public void Add(T item) {
        Inner.Add(item);
        CallOnChanged();
    }

    public void AddAll(IEnumerable<T> items) {
        bool any = false;

        foreach (var item in items) {
            Inner.Add(item);
            any = true;
        }

        if (any)
            CallOnChanged();
    }

    public void Clear() {
        Inner.Clear();
        CallOnChanged();
    }

    public bool Contains(T? item) => Inner.Contains(item!);

    public void CopyTo(T[] array, int arrayIndex) {
        Inner.CopyTo(array, arrayIndex);
    }

    public int IndexOf(T item) => Inner.IndexOf(item);

    public void Insert(int index, T item) {
        Inner.Insert(index, item);
        CallOnChanged();
    }

    public void RemoveAll(Func<T, bool> predicate) {
        bool changed = false;

        for (int i = Count - 1; i >= 0; i--) {
            var item = Inner[i];

            if (predicate(item)) {
                changed = true;
                Inner.RemoveAt(i);
            }
        }

        if (changed)
            CallOnChanged();
    }

    public bool Remove(T item) {
        if (Inner.Remove(item)) {
            CallOnChanged();
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) {
        Inner.RemoveAt(index);
        CallOnChanged();
    }

    public List<T>.Enumerator GetEnumerator() => Inner.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();
}
