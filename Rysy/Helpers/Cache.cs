﻿namespace Rysy.Helpers;

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

    /// <summary>
    /// Creates a new cache, whose value is generated by calling <paramref name="transform"/> on this cache's <see cref="Value"/>.
    /// This new cache will get invalided at the same time as this cache.
    /// </summary>
    public Cache<TNew> Chain<TNew>(Func<T, TNew> transform) where TNew : class? {
        //var token = new CacheToken();
        //Token.OnInvalidate += token.Invalidate;

        var cache = new Cache<TNew>(Token, () => transform(Value));

        return cache;
    }

    public void Clear() => _cached = null;
}
