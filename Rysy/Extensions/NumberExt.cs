using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rysy.Extensions;

public static class NumberExt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AtMost<T>(this T num, T max) where T : INumber<T> {
        return T.Min(num, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AtLeast<T>(this T num, T min) where T : INumber<T> {
        return T.Max(num, min);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SnapBetween<T>(this T num, T min, T max) where T : INumber<T> {
        return T.Max(min, T.Min(num, max));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Div<T>(this T num, T by) where T : INumber<T>
        => num / by;

    public static bool IsIn(this int i, Range range) {
        return (range.Start.IsFromEnd || range.Start.Value <= i)
            && (range.End.IsFromEnd || range.End.Value >= i);
    }

    /// <summary>
    /// Mathematical modulus, with correct handling for negative numbers.
    /// </summary>
    public static int MathMod(this int a, int b) {
        return (a % b + b) % b;
    }
}
