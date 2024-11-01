namespace Rysy.Helpers;

public ref struct SpanPair(ReadOnlySpan<char> a, ReadOnlySpan<char> b) {
    public readonly ReadOnlySpan<char> A = a;
    public readonly ReadOnlySpan<char> B = b;
}

/// <summary>
/// A equality comparer for a string tuple, allowing an alternate equality comparison with a pair of spans.
/// </summary>
public struct StringPairComparer : IEqualityComparer<(string,string)>, IAlternateEqualityComparer<SpanPair, (string,string)> {
    public bool Equals(SpanPair alternate, (string,string) other) {
        return alternate.A.SequenceEqual(other.Item1) && alternate.B.SequenceEqual(other.Item2);
    }

    public int GetHashCode(SpanPair alternate) {
        return HashCode.Combine(
            string.GetHashCode(alternate.A, StringComparison.Ordinal),
            string.GetHashCode(alternate.B, StringComparison.Ordinal)
        );
    }

    public (string,string) Create(SpanPair alternate) {
        return (alternate.A.ToString(), alternate.B.ToString());
    }

    public bool Equals((string, string) x, (string, string) y)
    {
        return x.Item1 == y.Item1 && x.Item2 == y.Item2;
    }

    public int GetHashCode((string, string) obj)
    {
        return HashCode.Combine(obj.Item1, obj.Item2);
    }
}