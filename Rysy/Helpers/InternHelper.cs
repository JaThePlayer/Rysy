using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// Helper class for interning boxed representations of simple types, used extensively in EntityData and placements.
/// </summary>
public static class InternHelper {
    private static readonly object True = true;
    private static readonly object False = false;
    
    public static object Intern(bool value) => value ? True : False;
    
    public static object Intern(int value) => InternHelper<int>.Intern(value);
    
    public static object Intern(float value) => InternHelper<float>.Intern(value);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(value))]
    public static object? TryIntern<T>(T value) {
        if (value is int i)
            return InternHelper<int>.Intern(i);
        if (value is float f)
            return InternHelper<float>.Intern(f);
        if (value is bool b)
            return Intern(b);
        if (value is string s)
            return string.Intern(s);

        return value;
    }
}

internal static class InternHelper<T> where T : struct, IEquatable<T> {
    private static volatile int _accesses = 0;
    private static readonly Dictionary<T, object> _cache = [];
    
    public static object Intern(T value) {
        Interlocked.Increment(ref _accesses);
        return _cache.TryGetValue(value, out var result) ? result : _cache[value] = value;
    }

    public static int AccessCount => _accesses;

    public static int CachedCount => _cache.Count;
}
