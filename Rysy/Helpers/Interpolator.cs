using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

/// <summary>
/// Provides a shared buffer for string interpolation, allowing for 0-allocation string interpolation into a ReadOnlySpan.
/// </summary>
public sealed class Interpolator {
    [ThreadStatic]
    private static Interpolator _shared;
    
    private char[] _buffer = new char[128];

    public char[] Buffer => _buffer;

    private int _startIndex;

    /// <summary>
    /// If false, subsequent calls to Interpolate will overwrite the buffer returned from the previous call.
    /// </summary>
    public bool ManualClear { get; set; } = false;

    /// <summary>
    /// Returns a ThreadStatic shared buffer
    /// </summary>
    public static Interpolator Shared => _shared ??= new();
    
    /// <summary>
    /// Returns a ThreadStatic, manually cleared, shared buffer
    /// </summary>
    public static Interpolator SharedManualClear => _shared ??= new() {
        ManualClear = true
    };

    /// <summary>
    /// Interpolates into a temporary buffer that will be overwritten the next time this method is called.
    /// </summary>
    public static ReadOnlySpan<char> Temp(Handler h)
        => Shared.Interpolate(h);
    
    /// <summary>
    /// Interpolates into a buffer that will only be overwritten the next time <see cref="ClearPreserved"/> is called.
    /// </summary>
    public static ReadOnlySpan<char> Preserved(Handler h)
        => SharedManualClear.Interpolate(h);

    public static void ClearPreserved() {
        SharedManualClear.Clear();
    }

    public ReadOnlySpan<char> Interpolate([InterpolatedStringHandlerArgument("")] Handler h)
    {
        _buffer = h.Data;
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += h.Result.Length;
        }
        
        return h.Result;
    }

    public Span<char> Clone(ReadOnlySpan<char> str) {
        if (str.Length == 0)
            return [];
        
        if (_buffer.Length < str.Length + _startIndex) {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 3 / 2, str.Length + _startIndex));
        }
        
        var b = _buffer.AsSpan()[_startIndex..];
        str.CopyTo(b);
        if (ManualClear) {
            // Move the start index further so that later interpolation steps don't overwrite existing ones
            _startIndex += str.Length;
        }

        return b[..str.Length];
    }

    /// <summary>
    /// If <see cref="ManualClear"/> is enabled, clears the buffer so that further interpolations start writing from the 0th index.
    /// </summary>
    public void Clear() {
        _startIndex = 0;
    }
    
    [InterpolatedStringHandler]
    public ref struct Handler {
        public Handler(int literalLength, int formattedCount, Interpolator buffer) {
            Data = buffer._buffer;
            _startIndex = buffer._startIndex;
        }

        public Handler(int literalLength, int formattedCount) : this(literalLength, formattedCount, Shared) { }

        private int _len;
        private readonly int _startIndex;
        internal char[] Data;
    
        public readonly int CapacityLeft => Data.Length - (_startIndex + _len);
        
        private Span<char> RemainingSpan() => Data.AsSpan(_startIndex + _len);

        private void Expand(int newSize)
        {
            Array.Resize(ref Data, newSize);
        }
        
        public ReadOnlySpan<char> Result => Data.AsSpan(0, _len);

        public int Length => _len;
        
        public void AppendLiteral(ReadOnlySpan<char> data)
        {
            if (data.Length > CapacityLeft)
            {
                Expand(Data.Length + data.Length);
            }
            
            data.CopyTo(RemainingSpan());
            _len += data.Length;
        }

        // Just to get rid of the implicit conversion marker in Rider
        public void AppendFormatted(string data)
            => AppendLiteral(data.AsSpan());
        
        public void AppendFormatted(ReadOnlySpan<char> str)
        {
            AppendLiteral(str);
        }
        
        public void AppendFormatted<T2>(T2 v) where T2 : ISpanFormattable
        {
            int written;
            while (!v.TryFormat(RemainingSpan(), out written, "", null))
            {
                Expand(Data.Length * 2);
            }
            
            _len += written;
        }
        
        public void AppendFormatted(object v) {
            AppendLiteral(v.ToString());
        }
    }
}