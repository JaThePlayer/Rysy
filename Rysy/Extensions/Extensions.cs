using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rysy;

public static class Extensions
{
    public static T[] AwaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        var all = Task.WhenAll(tasks);
        all.Wait();

        return all.Result;
    }

    public static void AwaitAll(this IEnumerable<Task> tasks)
    {
        Task.WaitAll(tasks.ToArray());
    }

    public static T Abs<T>(this T num) where T : INumber<T>
        => T.Abs(num);

    /// <summary>
    /// Fills the given 2d array with the specified value
    /// </summary>
    public static unsafe void Fill<T>(this T[,] tiles, T value) where T : unmanaged
    {
        fixed (T* tile = &tiles[0, 0])
        {
            new Span<T>(tile, tiles.Length).Fill(value);
        }
    }

    public static bool Contains<T>(this T[] tiles, T value) where T : unmanaged
    {
        return Array.IndexOf(tiles, value) >= 0;
    }

    public static IEnumerable<Task> SelectToTaskRun<T>(this IEnumerable<T> self, Action<T> action)
    {
        foreach (var item in self)
        {
            yield return Task.Run(() => action(item));
        }
    }

    public static IEnumerable<T> Do<T>(this IEnumerable<T> self, Action action)
    {
        action();
        foreach (var item in self)
        {
            yield return item;
        }
    }
}
