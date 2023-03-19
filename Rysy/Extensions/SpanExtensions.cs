namespace Rysy.Extensions;

public static class SpanExtensions {
    /// <summary>
    /// (Mutates in-place) Replaces all occurences of <paramref name="from"/> with <paramref name="with"/>
    /// </summary>
    public static void Replace(this Span<char> span, char from, char with) {
        int i;
        while ((i = span.IndexOf(from)) != -1) {
            span[i] = with;
            span = span[(i + 1)..];
        }
    }
}
