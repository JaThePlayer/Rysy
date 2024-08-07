﻿using System.Numerics;
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

    public static T ClampedMap<T>(this T val, T min, T max, T newMin, T newMax) where T : INumber<T> {
        return T.Clamp((val - min) / (max - min), T.Zero, T.One) * (newMax - newMin) + newMin;
    }
    
    public static T Clamp<T>(this T value, T min, T max) where T : INumber<T> {
        return T.Clamp(value, min, max);
    }

    public static T Floor<T>(this T num) where T : IFloatingPoint<T>
        => T.Floor(num);

    /// <summary>
    /// Snaps the angle (in degrees) to right angles (0, 90, 180, 270)
    /// </summary>
    public static float SnapAngleToRightAnglesDegrees(this float angleDegrees, float toleranceDegrees) {
        return angleDegrees.ToRad().SnapAngleToRightAnglesRad(toleranceDegrees.ToRad()).RadToDegrees();
    }

    /// <summary>
    /// Snaps the angle (in radians) to right angles (0, 90.ToRad(), 180.ToRad(), 270.ToRad())
    /// </summary>
    public static float SnapAngleToRightAnglesRad(this float angleRad, float toleranceRad) {
        if (toleranceRad < 0f) {
            toleranceRad = 0f;
        }

        angleRad %= 360f.ToRad();

        return Check(0f.ToRad())
            ?? Check(90f.ToRad())
            ?? Check(180f.ToRad())
            ?? Check(270f.ToRad())
            ?? angleRad;

        float? Check(float angleToCheck) {
            if (Math.Abs(angleToCheck - angleRad) <= toleranceRad) {
                return angleToCheck;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Formats a number into a filesize, like 1024 -> 1KB
    /// </summary>
    public static string ToFilesize(this long byteCount) {
        var suf = _filesizeAbbreviations;
        
        if (byteCount == 0)
            return "0" + suf[0];
        
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 2);
        
        return (Math.Sign(byteCount) * num) + suf[place];
    }

    /// <summary>
    /// Returns whether two boxed numbers are loosely equal, that is a float and int can be equal if their real values are the same.
    /// </summary>
    public static bool IntFloatLooselyEqual(object a, object b) {
        return (a, b) switch {
            (int ai, int bi) => ai == bi,
            (float af, int bi) => float.IsInteger(af) && (int)af == bi,
            (int ai, float bf) => float.IsInteger(bf) && ai == (int)bf,
            (float af, float bf) => af == bf,
            _ => false,
        };
    }

    private static readonly string[] _filesizeAbbreviations = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];  //Longs run out around EB
}
