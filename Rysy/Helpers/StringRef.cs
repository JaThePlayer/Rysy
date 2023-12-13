namespace Rysy.Helpers;

/// <summary>
/// Provides a way to create a string-keyed dictionary,
/// where the key can be created either a shared buffer, or a regular string, without any extra memory allocations from temporary keys.
/// If you're creating a StringRef from a shared buffer, make sure to not store that RefString as a key to a dictionary, to avoid it being mutated!
/// </summary>
public readonly struct StringRef : IEquatable<StringRef> {
    private readonly char[]? _srcCharBuffer;
    private readonly string? _srcString;

    private StringRef(char[]? charBuffer, string? srcString) {
        _srcCharBuffer = charBuffer;
        _srcString = srcString;
    }
    
    /// <summary>
    /// Returns the data represented by this reference.
    /// </summary>
    public ReadOnlySpan<char> Data => _srcString is {} srcString ? srcString.AsSpan() : _srcCharBuffer.AsSpan();

    /// <summary>
    /// Returns whether this instance wraps mutable data and should not be used as a dictionary key.
    /// </summary>
    public bool IsMutable => _srcCharBuffer is { };
    
    /// <summary>
    /// If this instance is mutable, creates a read-only clone of it. Otherwise, returns the same instance.
    /// </summary>
    public StringRef CloneIntoReadOnly() => _srcCharBuffer is { } shared ? Clone(shared) : this;

    /// <summary>
    /// Creates a StringRef which points to the provided buffer, without cloning.
    /// </summary>
    public static StringRef FromSharedBuffer(char[] shared) => new(shared, null);

    /// <summary>
    /// Creates a StringRef pointing to a copy of the provided data.
    /// If you have access to a string, call <see cref="FromString"/> instead to avoid cloning.
    /// </summary>
    public static StringRef Clone(ReadOnlySpan<char> str) => new(null, new string(str));
    
    /// <summary>
    /// Creates a StringRef pointing to the provided string. It will not be copied, as strings are immutable anyway.
    /// </summary>
    public static StringRef FromString(string str) => new(null, str);
        
    public bool Equals(StringRef other) {
        return Data.SequenceEqual(other.Data);
    }

    public override int GetHashCode() => string.GetHashCode(Data);
        
    public override bool Equals(object? obj)
        => obj is StringRef other && Equals(other);
}