using Rysy.Extensions;

namespace Rysy;

/// <summary>
/// Specifies a direction in which a selection got rotated. Left = -1, Right = 1
/// </summary>
public enum RotationDirection {
    Left = -1,
    Right = 1
}

public static class RotationDirectionExtensions {
    /// <summary>
    /// Adds or subtracts 1 from <paramref name="enumVal"/> based on <paramref name="rotDir"/>, then uses MathMod to make the resulting value in-bounds of T, for implementing rotations.
    /// </summary>
    public static T AddRotationTo<T>(this RotationDirection rotDir, T enumVal) where T : struct, Enum {
        var count = Enum.GetValues<T>().Length;

        var newVal = (Convert.ToInt32(enumVal) + (int) rotDir).MathMod(count);
        return (T) Enum.ToObject(typeof(T), newVal);
    }
}