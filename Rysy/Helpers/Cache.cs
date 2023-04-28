namespace Rysy.Helpers;

public class Cache<T> where T : class? {
    public readonly CacheToken Token;
    public readonly Func<T> Generator;

    private T? _cached;

    public Cache(CacheToken token, Func<T> generator) {
        Token = token;
        Generator = generator;

        token.OnInvalidate += () => {
            _cached = null;
        };
    }

    public T Value => _cached ??= Generator();
}
