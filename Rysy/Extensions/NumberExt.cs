using Microsoft.CodeAnalysis;
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

    public static int SnapToGrid(this int num, int gridSize) {
        return num / gridSize * gridSize;
    }

    public static float SnapToGrid(this float num, float gridSize) {
        return float.Floor(num / gridSize) * gridSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Div<T>(this T num, T by) where T : INumber<T>
        => num / by;

    public static bool IsInRange(this int i, Range range) {
        return (range.Start.IsFromEnd || range.Start.Value <= i)
            && (range.End.IsFromEnd || range.End.Value >= i);
    }

    public static bool IsInRange<T>(this T num, T min, T max) where T : INumber<T> {
        return num >= min && num <= max;
    }

    /// <summary>
    /// Mathematical modulus, with correct handling for negative numbers.
    /// </summary>
    public static int MathMod(this int a, int b) {
        return (a % b + b) % b;
    }

    public static T Map<T>(this T num, T min, T max, T newMin, T newMax) where T : INumber<T> {
        var slope = (newMax - newMin) / (max - min);
        return min + slope * (num - min);
    }

    public static T Floor<T>(this T num) where T : IFloatingPoint<T>
        => T.Floor(num);
}
