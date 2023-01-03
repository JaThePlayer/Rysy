namespace Rysy.Helpers;

/// <summary>
/// A token wrapping over a function that gets called when the cache gets invalidated.
/// </summary>
public class CacheToken
{
    public bool Valid { get; private set; }

    public Action OnInvalidate;

    public CacheToken(Action onInvalidate) {
        OnInvalidate = onInvalidate;
    }

    public void Invalidate() {
        if (Valid) {
            Valid = false;
            OnInvalidate();
        }
    }

    public void Reset() => Valid = true;
}
