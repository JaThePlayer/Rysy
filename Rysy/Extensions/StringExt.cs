using Rysy.Extensions;
using System.Text.RegularExpressions;

namespace Rysy.Extensions;

public static partial class StringExt {
    //[GeneratedRegex("[a-z][A-Z]")]
    public static Regex PascalCaseRegex = new("[a-z][A-Z]", RegexOptions.Compiled);

    //[GeneratedRegex(@"[A-Z]:[/\\]Users[/\\](.*?)[/\\]")]
    public static Regex UserNameRegex = new(@"[A-Z]:[/\\]Users[/\\](.*?)[/\\]", RegexOptions.Compiled);

    private static Regex UnformatRegex = new(@"\u001b\[[^m]{1,2}m", RegexOptions.Compiled);

    /// <summary>
    /// Splits the string on [a-z][A-Z] patterns, inserting a space between them.
    /// </summary>
    public static string SplitPascalCase(this string pascalCase) {
        return PascalCaseRegex.Replace(pascalCase, (match) => {
            return $"{match.ValueSpan[0]} {match.ValueSpan[1]}";
        });
    }


    /// <summary>
    /// Trims a piece of text from the end of the string
    /// </summary>
    public static string TrimEnd(this string from, string elem, StringComparison comp = StringComparison.InvariantCulture) {
        if (from.EndsWith(elem, comp))
            return from[..^elem.Length];
        return from;
    }

    /// <summary>
    /// Trims a piece of text from the start of the string
    /// </summary>
    public static string TrimStart(this string from, string elem) {
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
    /// Tries to censor personal info from filepaths, specifically windows usernames.
    /// Should be used when a path is displayed on-screen
    /// </summary>
    public static string Censor(this string path) {
        if (string.IsNullOrWhiteSpace(path))
            return path;


        if (UserNameRegex.Matches(path) is { } matches) {
            for (int i = 0; i < matches.Count; i++)
                path = path.Replace(matches[i].Groups[1].Value, "<USER>");
            return path;
        }

        return path;
    }

    /// <summary>
    /// Calls <see cref="Path.GetDirectoryName(string?)"/> on this string
    /// </summary>
    public static string? Directory(this string? path) {
        return Path.GetDirectoryName(path);
    }

    /// <summary>
    /// Calls <see cref="Path.GetFileNameWithoutExtension(string?)"/> on this string
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? FilenameNoExt(this string? path) {
        return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Calls <see cref="Regex.Replace(string, string)"/> with the provided regex with the given strings
    /// </summary>
    public static string RegexReplace(this string from, Regex regex, string with)
        => regex.Replace(from, with);

    public static string LowercaseFirst(this string from) {
        return string.Create(from.Length, from, static (newstr, from) => {
            from.TryCopyTo(newstr);
            newstr[0] = char.ToLower(from[0]);
        });
    }

    public static string UppercaseFirst(this string from) {
        return string.Create(from.Length, from, static (newstr, from) => {
            from.TryCopyTo(newstr);
            newstr[0] = char.ToUpper(from[0]);
        });
    }

    /// <summary>
    /// Combines <see cref="SplitPascalCase(string)"/> and <see cref="UppercaseFirst(string)"/>
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Humanize(this string text) => text.TrimStart('_').SplitPascalCase().UppercaseFirst();

    /// <summary>
    /// Returns the string <paramref name="formatted"/> with all color formatting removed.
    /// </summary>
    public static string UnformatColors(this string formatted) => formatted.RegexReplace(UnformatRegex, "");

    /// <summary>
    /// Returns <paramref name="possiblyDuplicated"/> suffixed in such a way that the returned string is not contained in <paramref name="strings"/>.
    /// </summary>
    public static string GetDeduplicatedIn(this string possiblyDuplicated, IEnumerable<string> strings) {
        int i = 0;
        var origName = possiblyDuplicated;
        var newName = origName;

        while (strings.Contains(newName)) {
            newName = $"{origName}-{i}";
            i++;
        }

        return newName;
    }

    public static string ToValidFilename(this string str) {
        if (str == null)
            return string.Empty;

        return str.ReplaceAll(Path.GetInvalidFileNameChars(), '_');
    }

    public static string ReplaceAll(this string str, char[] chars, char replacement) {
        return string.Create(str.Length, (str, chars, replacement), static (span, state) => {
            var (str, chars, replacement) = state;
            str.AsSpan().CopyTo(span);

            int i;
            while ((i = span.IndexOfAny(chars)) != -1) {
                span[i] = replacement;
                span = span[i..];
            }
        });
    }

    public static string ToVirtPath(this string val, string prefix = "")
        => ToVirtPath(val.AsSpan(), prefix);

    public static string ToVirtPath(this ReadOnlySpan<char> val, string prefix = "") {
        var ext = Path.GetExtension(val);
        if (ext.IsEmpty)
            return val.ToString();

        // Trim trailing slash
        var vLen = val.Length - ext.Length;
        if (val.Length > 1 && val[vLen - 1] is '\\' or '/') {
            vLen--;
        }

        Span<char> b = stackalloc char[vLen];
        val[0..vLen].CopyTo(b);
        b.ReplaceInPlace('\\', '/');

        if (!string.IsNullOrWhiteSpace(prefix))
            return $"{prefix}:{b}";

        return b.ToString();
    }
}
