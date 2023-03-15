using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rysy;

public static class Extensions {
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

    public static int AsInt(this bool b) => Unsafe.As<bool, byte>(ref b);
    public static byte AsByte(this bool b) => Unsafe.As<bool, byte>(ref b);

    public static NumVector4 ToNumVec4(this Color color) => color.ToVector4().ToNumerics();

    public static T[] ShallowClone<T>(this T[] array) => (T[])array.Clone();
}
