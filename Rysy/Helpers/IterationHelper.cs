namespace Rysy.Helpers;

public static class IterationHelper {
    public static string[] EachName<TEnum>() where TEnum : struct, Enum 
        => Enum.GetNames<TEnum>();

    public static IEnumerable<string> EachNameToLower<TEnum>() where TEnum : struct, Enum
        => Enum.GetNames<TEnum>().Select(n => n.ToLowerInvariant());

    public static IEnumerable<(T1, T2)> EachPair<T1, T2>(IEnumerable<T1> a, IEnumerable<T2> b) {
        return a.SelectMany(a1 => b.Select(b1 => (a1, b1)));
    }

    public static IEnumerable<(T1, T2, T3)> EachPair<T1, T2, T3>(IEnumerable<T1> a, IEnumerable<T2> b, IEnumerable<T3> c) {
        return a.SelectMany(a1 => b.SelectMany(b1 => c.Select(c1 => (a1, b1, c1))));
    }

    public static bool[] BoolValues => new[] { false, true };
}
