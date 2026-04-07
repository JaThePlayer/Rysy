using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

/// <summary>
/// Interface which provides a default implementation of the redundant methods in <see cref="ISpanParsable{TSelf}"/>.
/// </summary>
internal interface ISimpleParsable<T> : ISpanParsable<T> where T : ISimpleParsable<T> {
    static T IParsable<T>.Parse(string s, IFormatProvider? provider) {
        return T.Parse(s.AsSpan(), provider);
    }

    static bool IParsable<T>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out T result) {
        return T.TryParse(s.AsSpan(), provider, out result);
    }

    static T ISpanParsable<T>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
        if (T.TryParse(s, provider, out var result))
            return result;
        
        throw new FormatException($"Unable to parse \"{s}\" to type {typeof(T).Name}");
    }
}
