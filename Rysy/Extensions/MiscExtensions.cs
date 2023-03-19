using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rysy.Extensions;

public static class MiscExtensions {
    public static T[] AwaitAll<T>(this IEnumerable<Task<T>> tasks) {
        var all = Task.WhenAll(tasks);
        all.Wait();

        return all.Result;
    }

    public static void AwaitAll(this IEnumerable<Task> tasks) {
        Task.WaitAll(tasks.ToArray());
    }

    public static T Abs<T>(this T num) where T : INumber<T>
        => T.Abs(num);

    public static bool Contains<T>(this T[] tiles, T value) where T : unmanaged {
        return Array.IndexOf(tiles, value) >= 0;
    }

    /// <summary>
    /// If this is true, then performs the action
    /// </summary>
    public static void IfTrue(this bool val, Action act) {
        if (val)
            act();
    }

    /// <summary>
    /// Performs <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> to reinterpret this bool to a byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte AsByte(this bool b) => Unsafe.As<bool, byte>(ref b);

    /// <summary>
    /// Converts this color to a <see cref="NumVector3"/>[R, G, B]
    /// </summary>
    public static NumVector3 ToNumVec3(this Color color) => color.ToVector3().ToNumerics();

    /// <summary>
    /// Converts this color to a <see cref="NumVector3"/>[R, G, B]
    /// </summary>
    public static NumVector4 ToNumVec4(this Color color) => color.ToVector4().ToNumerics();

    public static T[] ShallowClone<T>(this T[] array) => (T[]) array.Clone();
}
