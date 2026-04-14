using System.Collections;
using System.ComponentModel;

namespace Rysy.Helpers;

/// <summary>
/// Data for the <see cref="IReadOnlyListenableList{T}.OnChanged"/> event.
/// </summary>
/// <param name="Action">An action that specifies how the collection changed.</param>
/// <param name="Item">The item that was added/removed. Null if the collection was refreshed.</param>
/// <typeparam name="T">The type of the collection changed.</typeparam>
public record struct ListenableListChanged<T>(CollectionChangeAction Action, T? Item);

public interface IListenableList<T> : IList<T>, IReadOnlyListenableList<T> {
    // These are to resolve conflicts between IList<T> and IReadOnlyList<T>

    /// <inheritdoc cref="IList{T}.Count" />
    public new int Count { get; }
    
    /// <inheritdoc cref="IList{T}.this" />
    public new T this[int v] {
        get;
        set;
    }
}

public interface IReadOnlyListenableList<T> : IReadOnlyList<T> {
    /// <summary>
    /// Will be called whenever the contents of the list get changed (Elements get added/removed)
    /// </summary>
    public Action<ListenableListChanged<T>>? OnChanged { get; set; }
    
    /// <summary>
    /// Current version of the list, incremented each time OnChanged is called.
    /// </summary>
    public long Version { get; }
}

/// <summary>
/// Acts like a <see cref="List{T}"/>, but implements <see cref="IListenableList{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListenableList<T> : IListenableList<T> {
    private List<T> _inner;
    public Action<ListenableListChanged<T>>? OnChanged { get; set; }
    
    public long Version { get; private set; }

    private bool _suppressed;

    public void SuppressCallbacks() {
        _suppressed = true;
    }

    public void Unsuppress() {
        _suppressed = false;
    }
    
    protected void CallOnChanged(ListenableListChanged<T> changed) {
        if (!_suppressed) {
            Version++;
            OnChanged?.Invoke(changed);
        }
    }

    public ListenableList() {
        _inner = [];
    }

    public ListenableList(Action onChanged) {
        _inner = [];

        OnChanged = _ => onChanged();
    }

    public ListenableList(Action onChanged, int capacity) {
        _inner = new List<T>(capacity);

        OnChanged = _ => onChanged();
    }
    
    public ListenableList(Action<ListenableListChanged<T>> onChanged) {
        _inner = [];

        OnChanged = onChanged;
    }

    public ListenableList(Action<ListenableListChanged<T>> onChanged, int capacity) {
        _inner = new List<T>(capacity);

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
            var old = _inner[index];
            _inner[index] = value;
            CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, old));
            CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Add, value));
        }
    }

    public int Count => _inner.Count;

    public bool IsReadOnly => false;


    public void Add(T item) {
        _inner.Add(item);
        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Add, item));
    }

    public void AddAll(IEnumerable<T> items) {
        foreach (var item in items) {
            Add(item);
        }
    }

    public void Clear() {
        _inner.Clear();
        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Refresh, default));
    }

    public bool Contains(T? item) => _inner.Contains(item!);

    public void CopyTo(T[] array, int arrayIndex) {
        _inner.CopyTo(array, arrayIndex);
    }

    public int IndexOf(T item) => _inner.IndexOf(item);

    public void Insert(int index, T item) {
        _inner.Insert(index, item);
        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Add, item));
    }

    public void RemoveAll(Func<T, bool> predicate) {
        for (int i = Count - 1; i >= 0; i--) {
            var item = _inner[i];

            if (predicate(item)) {
                RemoveAt(i);
            }
        }
    }

    public bool Remove(T item) {
        if (_inner.Remove(item)) {
            CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, item));
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) {
        var item = _inner[index];
        _inner.RemoveAt(index);
        CallOnChanged(new ListenableListChanged<T>(CollectionChangeAction.Remove, item));
    }

    public List<T>.Enumerator GetEnumerator() => _inner.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}
