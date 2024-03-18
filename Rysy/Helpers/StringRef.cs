using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// Same as Span, but is not a ref-struct and as such is unsafe.
/// </summary>
public readonly unsafe struct UnsafeSpan<T> where T : unmanaged {
    private readonly T* _first;
    private readonly int _length;

    public UnsafeSpan(T* first, int length) {
        _first = first;
        _length = length;
    }

    public UnsafeSpan(T[] array) {
        if (array.Length == 0) {
            _first = null;
        } else {
            _first = (T*)Unsafe.AsPointer(ref array[0]);
        }
        _length = array.Length;
    }

    public UnsafeSpan(ReadOnlySpan<T> span) {
        if (span.Length == 0) {
            _first = null;
        } else {
            _first = (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in span[0]));
        }
        _length = span.Length;
    }

    public Span<T> AsSpan() => new(_first, _length);

    public bool Empty => _length == 0;
}

/// <summary>
/// Provides a way to create a string-keyed dictionary,
/// where the key can be created either a shared buffer, or a regular string, without any extra memory allocations from temporary keys.
/// If you're creating a StringRef from a shared buffer, make sure to not store that RefString as a key to a dictionary, to avoid it being mutated!
/// </summary>
public readonly struct StringRef : IEquatable<StringRef> {
    private readonly char[]? _srcCharBuffer;
    private readonly int _srcCharBufferLen;
    
    private readonly string? _srcString;
    //private readonly UnsafeSpan<char> _srcUnsafeSpan;

    private StringRef(char[]? charBuffer, string? srcString) {
        _srcCharBuffer = charBuffer;
        _srcString = srcString;
        _srcCharBufferLen = charBuffer?.Length ?? 0;
    }
    
    private StringRef(char[] charBuffer, int len) {
        _srcCharBuffer = charBuffer;
        _srcString = null;
        _srcCharBufferLen = len;
    }
    
    /// <summary>
    /// Returns the data represented by this reference.
    /// </summary>
    public ReadOnlySpan<char> Data => _srcString is {} srcString ? srcString.AsSpan() : _srcCharBuffer.AsSpan(0, _srcCharBufferLen);

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
    /// Creates a StringRef which points to the provided buffer, without cloning.
    /// </summary>
    public static StringRef FromSharedBuffer(char[] shared, int len) => new(shared, len);

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

    public static bool operator ==(StringRef left, StringRef right) {
        return left.Equals(right);
    }

    public static bool operator !=(StringRef left, StringRef right) {
        return !(left == right);
    }

    public override string ToString() {
        return _srcString ?? Data.ToString();
    }
}