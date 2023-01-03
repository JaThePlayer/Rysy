using System.Text.RegularExpressions;

namespace Rysy;

public static partial class StringExt
{
    [GeneratedRegex("[a-z][A-Z]")]
    public static partial Regex PascalCaseRegex();

    [GeneratedRegex(@"[A-Z]:[/\\]Users[/\\](.*?)[/\\]")]
    public static partial Regex UserNameRegex();

    /// <summary>
    /// Splits the string on [a-z][A-Z] patterns, inserting a space between them.
    /// </summary>
    public static string SplitPascalCase(this string pascalCase)
    {
        return PascalCaseRegex().Replace(pascalCase, (Match match) =>
        {
            return $"{match.ValueSpan[0]} {match.ValueSpan[1]}";
        });
    }


    /// <summary>
    /// Trims a piece of text from the end of the string
    /// </summary>
    public static string TrimEnd(this string from, string elem, StringComparison comp = StringComparison.InvariantCulture)
    {
        if (from.EndsWith(elem, comp))
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
        => path switch
        {
            null => "",
            _ => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar),
        };

    /// <summary>
    /// Tries to censor personal info from filepaths, specifically windows usernames.
    /// Should be used when a path is displayed on-screen
    /// </summary>
    public static string TryCensor(this string path) {
        if (UserNameRegex().Match(path) is { } m) {
            return path.Replace(m.Groups[1].Value, "<USER>");
        }

        return path;
    }

    /// <summary>
    /// Calls <see cref="Regex.Replace(string, string)"/> with the provided regex with the given strings
    /// </summary>
    public static string RegexReplace(this string from, Regex regex, string with)
        => regex.Replace(from, with);
}
