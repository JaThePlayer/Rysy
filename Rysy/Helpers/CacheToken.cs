namespace Rysy.Helpers;

/// <summary>
/// A token wrapping over a function that gets called when the cache gets invalidated.
/// </summary>
public class CacheToken {
    public bool Valid { get; private set; }

    /// <summary>
    /// Called when the <see cref="Invalidate"/> method gets called and <see cref="Valid"/> is true.
    /// </summary>
    public Action? OnInvalidate;

    /// <summary>
    /// Called when this token gets disposed of or GC'd
    /// </summary>
    public Action? OnDispose;

    public CacheToken() { }

    public CacheToken(Action onInvalidate) {
        OnInvalidate = onInvalidate;
    }

    public void Invalidate() {
        if (Valid) {
            Valid = false;
            OnInvalidate?.Invoke();
        }
    }

    public void Reset() => Valid = true;

    public Cache<T> CreateCache<T>(Func<T> generator) where T : class? {
        return new(this, generator);
    }

    ~CacheToken() {
        OnDispose?.Invoke();
    }
}
