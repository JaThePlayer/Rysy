using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rysy;

public static class NumberExt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Cap<T>(this T num, T max) where T : INumber<T> {
        return T.Min(num, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Div<T>(this T num, T by) where T : INumber<T>
        => num / by;
}
