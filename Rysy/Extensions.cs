using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Trims a piece of text from the end of the string
    /// </summary>
    public static string TrimEnd(this string from, string elem)
    {
        if (from.EndsWith(elem))
            return from[..^elem.Length];
        return from;
    }

    /// <summary>
    /// Trims a piece of text from the start of the string
    /// </summary>
    public static string TrimStart(this string from, string elem)
    {
        if (from.StartsWith(elem))
            return from[elem.Length..];
        return from;
    }

    /// <summary>
    /// Replaces backslashes with slashes in the given string
    /// </summary>
    public static string Unbackslash(this string from)
        => from.Replace('\\', '/');

    /// <summary>
    /// Corrects the slashes in the given path to be correct for the given OS.
    /// Since all OS'es seem to support forward slashes, only use this for printing!
    /// </summary>
    public static string CorrectSlashes(this string path)
        => path switch { 
            null => "",
            _ => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar),
        };

    /// <summary>
    /// Calls <see cref="Regex.Replace(string, string)"/> with the provided regex with the given strings
    /// </summary>
    /// <param name="from"></param>
    /// <param name="regex"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    public static string RegexReplace(this string from, Regex regex, string with)
        => regex.Replace(from, with);


    public static T Abs<T>(this T num) where T : INumber<T>
        => T.Abs(num);


    public static Vector2 XY(this Rectangle r) => new(r.X, r.Y);

    /// <summary>
    /// Returns new <see cref="Vector2"/>(<paramref name="r"/>.Width, <paramref name="r"/>.Height)
    /// </summary>
    public static Vector2 WH(this Rectangle r) => new(r.Width, r.Height);

    public static Vector2 AddX(this Vector2 v, float toAdd) => new(v.X + toAdd, v.Y);
    public static Vector2 AddY(this Vector2 v, float toAdd) => new(v.X, v.Y + toAdd);

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

    public static float Angle(Vector2 from, Vector2 to)
        => float.Atan2(to.Y - from.Y, to.X - from.X);

    public static Vector2 AngleToVector(float angleRadians, float length)
        => new(float.Cos(angleRadians) * length, float.Sin(angleRadians) * length);

}
