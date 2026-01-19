using System.Collections;

namespace Rysy.Helpers;

public interface IListenableList<T> : IList<T> {
    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action? OnChanged { get; set; }
    
    /// <summary>
    /// Current version of the list, incremented each time OnChanged is called.
    /// </summary>
    public long Version { get; }
}

public interface IReadOnlyListenableList<T> : IReadOnlyList<T> {
    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action? OnChanged { get; set; }
    
    /// <summary>
    /// Current version of the list, incremented each time OnChanged is called.
    /// </summary>
    public long Version { get; }
}

/// <summary>
/// Acts like a <see cref="List{T}"/>, but implements <see cref="IListenableList{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListenableList<T> : IListenableList<T>, IReadOnlyListenableList<T> {
    private List<T> _inner;
    public Action? OnChanged { get; set; }
    
    public long Version { get; private set; }

    private bool _suppressed;

    public void SuppressCallbacks() {
        _suppressed = true;
    }

    public void Unsuppress() {
        _suppressed = false;
    }
    
    protected void CallOnChanged() {
        if (!_suppressed) {
            Version++;
            OnChanged?.Invoke();
        }
    }

    public ListenableList() {
        _inner = new();
    }

    public ListenableList(Action onChanged) {
        _inner = new();

        OnChanged = onChanged;
    }

    public ListenableList(Action onChanged, int capacity) {
        _inner = new(capacity);

        OnChanged = onChanged;
    }

    public ListenableList(int capacity) {
        _inner = new(capacity);
    }

    public ListenableList(IEnumerable<T> from) {
        _inner = new(from);
    }

    public T this[int index] {
        get => _inner[index];
        set {
            _inner[index] = value;
            CallOnChanged();
        }
    }

    public int Count => _inner.Count;

    public bool IsReadOnly => false;


    public void Add(T item) {
        _inner.Add(item);
        CallOnChanged();
    }

    public void AddAll(IEnumerable<T> items) {
        bool any = false;

        foreach (var item in items) {
            _inner.Add(item);
            any = true;
        }

        if (any)
            CallOnChanged();
    }

    public void Clear() {
        _inner.Clear();
        CallOnChanged();
    }

    public bool Contains(T? item) => _inner.Contains(item!);

    public void CopyTo(T[] array, int arrayIndex) {
        _inner.CopyTo(array, arrayIndex);
    }

    public int IndexOf(T item) => _inner.IndexOf(item);

    public void Insert(int index, T item) {
        _inner.Insert(index, item);
        CallOnChanged();
    }

    public void RemoveAll(Func<T, bool> predicate) {
        bool changed = false;

        for (int i = Count - 1; i >= 0; i--) {
            var item = _inner[i];

            if (predicate(item)) {
                changed = true;
                _inner.RemoveAt(i);
            }
        }

        if (changed)
            CallOnChanged();
    }

    public bool Remove(T item) {
        if (_inner.Remove(item)) {
            CallOnChanged();
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) {
        _inner.RemoveAt(index);
        CallOnChanged();
    }

    public List<T>.Enumerator GetEnumerator() => _inner.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}
