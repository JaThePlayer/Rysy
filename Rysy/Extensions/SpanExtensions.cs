using System.Collections;

namespace Rysy.Extensions;

public static class SpanExtensions {
    #if !NET8_0_OR_GREATER
    /// <summary>
    /// Shim for Span.Replace from .net8
    /// </summary>
    public static void Replace(this Span<char> span, char from, char with) {
        int i;
        while ((i = span.IndexOf(from)) != -1) {
            span[i] = with;
            span = span[(i + 1)..];
        }
    }
    #endif

    /// <summary>
    /// Enumerates each split in the <paramref name="span"/>, without any memory allocations
    /// </summary>
    public static SpanSplitEnumerator EnumerateSplits(this ReadOnlySpan<char> span, char sep) => new(span, sep);

    public ref struct SpanSplitEnumerator {
        private ReadOnlySpan<char> _remaining;
        private ReadOnlySpan<char> _current;
        private bool _isEnumeratorActive;
        private char _sep;

        internal SpanSplitEnumerator(ReadOnlySpan<char> buffer, char sep)
        {
            _remaining = buffer;
            _current = default;
            _isEnumeratorActive = true;
            _sep = sep;
        }
        
        public ReadOnlySpan<char> Current => _current;
        
        public SpanSplitEnumerator GetEnumerator() => this;
        
        public bool MoveNext()
        {
            if (!_isEnumeratorActive)
            {
                return false; // EOF previously reached or enumerator was never initialized
            }

            int idx = _remaining.IndexOf(_sep);
            if (idx >= 0)
            {
                _current = _remaining[..idx];
                _remaining = _remaining[(idx + 1)..];
            }
            else
            {
                // We've reached EOF, but we still need to return 'true' for this final
                // iteration so that the caller can query the Current property once more.

                _current = _remaining;
                _remaining = default;
                _isEnumeratorActive = false;
            }

            return true;
        }
    }
}
