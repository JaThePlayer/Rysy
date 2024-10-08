using Rysy.Extensions;
using Rysy.Helpers;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Rysy.Extensions;

public static partial class StringExt {
    //[GeneratedRegex("[a-z][A-Z]")]
    public static Regex PascalCaseRegex { get; } = new("[a-z][A-Z]", RegexOptions.Compiled);

    //[GeneratedRegex(@"[A-Z]:[/\\]Users[/\\](.*?)[/\\]")]
    public static Regex UserNameRegex { get; } = new(@"[A-Z]:[/\\]Users[/\\](.*?)[/\\]", RegexOptions.Compiled);

    private static Regex UnformatRegex { get; } = new(@"\u001b\[[^m]{1,2}m", RegexOptions.Compiled);

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
        if (from.StartsWith(elem, StringComparison.Ordinal))
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
                path = path.Replace(matches[i].Groups[1].Value, "<USER>", StringComparison.Ordinal);
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
    /// Calls <see cref="Path.GetExtension(string?)"/> on this string
    /// </summary>
    public static string? FileExtension(this string? path) {
        return Path.GetExtension(path);
    }

    /// <summary>
    /// Calls <see cref="Regex.Replace(string, string)"/> with the provided regex with the given strings
    /// </summary>
    public static string RegexReplace(this string from, Regex regex, string with)
        => regex.Replace(from, with);

    /// <summary>
    /// Makes each word in the given string start with an lowercase char
    /// </summary>
    public static string LowercaseFirst(this string from) {
        return string.Create(from.Length, from, static (newstr, from) => {
            from.TryCopyTo(newstr);
            newstr[0] = char.ToLowerInvariant(from[0]);

            int i;
            while ((i = newstr.IndexOf(' ') + 1) != 0 && i < newstr.Length) {
                newstr[i] = char.ToLowerInvariant(newstr[i]);
                newstr = newstr[i..];
            }
        });
    }

    /// <summary>
    /// Makes each word in the given string start with an uppercase char
    /// </summary>
    public static string UppercaseFirst(this string from) {
        return string.Create(from.Length, from, static (newstr, from) => {
            from.TryCopyTo(newstr);
            newstr[0] = char.ToUpperInvariant(from[0]);

            int i;
            while ((i = newstr.IndexOf(' ') + 1) != 0 && i < newstr.Length) {
                newstr[i] = char.ToUpperInvariant(newstr[i]);
                newstr = newstr[i..];
            }
        });
    }

    private static char[] HumanizeReplacedChars = new[] { '.', '_'};

    private static Dictionary<string, string> _humanizeCache = new();

    /// <summary>
    /// Combines <see cref="SplitPascalCase(string)"/> and <see cref="UppercaseFirst(string)"/>
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Humanize(this string? text) {
        if (text is null)
            return "";
        
        ref var humanized = ref CollectionsMarshal.GetValueRefOrAddDefault(_humanizeCache, text, out var exists);
        humanized ??= text?.TrimStart('_').ReplaceAll(HumanizeReplacedChars, ' ').SplitPascalCase().UppercaseFirst() ?? "";

        return humanized;
    }

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

        var stringsHashSet = strings.ToHashSet(StringComparer.Ordinal);

        while (stringsHashSet.Contains(newName)) {
            newName = $"{origName}-{i}";
            i++;
        }

        return newName;
    }


    /// <summary>
    /// Converts the given string to create a string that's a valid filename, by replacing all illegal characters with an underscore.
    /// </summary>
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
        b.Replace('\\', '/');

        if (!string.IsNullOrWhiteSpace(prefix))
            return $"{prefix}:{b}";

        return b.ToString();
    }

    /// <summary>
    /// Translates the string using <see cref="LangRegistry.Translate(string)"/>.
    /// If no translation is found, returns the string itself.
    /// </summary>
    public static string Translate(this string str) => LangRegistry.Translate(str);

    /// <summary>
    /// Translates the string using <see cref="LangRegistry.Translate(string)"/>, then formats it using <see cref="string.Format(string, object?[])"/>
    /// If no translation is found, formats the string itself.
    /// </summary>
    public static string TranslateFormatted(this string str, params object[] args) => string.Format(CultureInfo.CurrentCulture, LangRegistry.Translate(str), args);

    /// <summary>
    /// Translates the string using <see cref="LangRegistry.TranslateOrNull(string)"/>.
    /// If no translation is found, returns null.
    /// </summary>
    public static string? TranslateOrNull(this string str) => LangRegistry.TranslateOrNull(str);
    
    /// <summary>
    /// Translates the string using <see cref="LangRegistry.TranslateOrNull(string)"/>.
    /// If no translation is found, returns null.
    /// </summary>
    public static string? TranslateOrNull(this ReadOnlySpan<char> str) => LangRegistry.TranslateOrNull(str);

    /// <summary>
    /// Translates the string $"{<paramref name="prefix"/>}.{<paramref name="str"/>}" using <see cref="LangRegistry.TranslateOrNull(string)"/>.
    /// If no translation is found, returns null.
    /// </summary>
    public static string? TranslateOrNull(this string str, string prefix) => LangRegistry.TranslateOrNull($"{prefix}.{str}");
    
    /// <summary>
    /// <inheritdoc cref="TranslateOrNull(string, string)"/>
    /// </summary>
    public static string? TranslateOrNull(this ReadOnlySpan<char> str, ReadOnlySpan<char> prefix) => LangRegistry.TranslateOrNull($"{prefix}.{str}");

    /// <summary>
    /// Translates the string $"{<paramref name="prefix"/>}.{<paramref name="str"/>}" using <see cref="LangRegistry.TranslateOrNull(string)"/>.
    /// If no translation is found, returns the result of calling <see cref="Humanize(string)"/> on <paramref name="str"/>
    /// </summary>
    public static string TranslateOrHumanize(this string str, string prefix) => TranslateOrNull(str, prefix) ?? Humanize(str);
    
    public static string TranslateOrHumanize(this string str, ReadOnlySpan<char> prefix) => TranslateOrNull(str, prefix) ?? Humanize(str);

    public static int ToInt(this string s) => Convert.ToInt32(s, CultureInfo.InvariantCulture);
    public static int ToIntHex(this string s) => int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    public static uint ToUInt(this string s) => Convert.ToUInt32(s, CultureInfo.InvariantCulture);
    public static uint ToUIntHex(this string s) => uint.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    public static short ToShort(this string s) => Convert.ToInt16(s, CultureInfo.InvariantCulture);
    public static ushort ToUShort(this string s) => Convert.ToUInt16(s, CultureInfo.InvariantCulture);
    public static byte ToByte(this string s) => Convert.ToByte(s, CultureInfo.InvariantCulture);
    public static sbyte ToSByte(this string s) => Convert.ToSByte(s, CultureInfo.InvariantCulture);
    public static float ToSingle(this string s) => Convert.ToSingle(s, CultureInfo.InvariantCulture);
    public static double ToDouble(this string s) => Convert.ToDouble(s, CultureInfo.InvariantCulture);
    public static decimal ToDecimal(this string s) => Convert.ToDecimal(s, CultureInfo.InvariantCulture);

    public static float ToSingle(this ReadOnlySpan<char> s) => float.Parse(s, CultureInfo.InvariantCulture);
    public static float ToSingle(this Span<char> s) => float.Parse(s, CultureInfo.InvariantCulture);
    public static float ToInt(this ReadOnlySpan<char> s) => int.Parse(s, CultureInfo.InvariantCulture);

    public static bool IsNullOrWhitespace(this string? s) => string.IsNullOrWhiteSpace(s);

    /// <summary>
    /// Counts how many times the character <paramref name="x"/> appears in the given string.
    /// </summary>
    public static int CountFast(this string s, char x) {
        var sum = 0;
        var i = -1;
        while ((i = s.IndexOf(x, i + 1)) != -1) {
            sum++;
        }
        return sum;
    }

    public static string TrimPrefix(this string s, ReadOnlySpan<char> prefix) {
        if (s.AsSpan().StartsWith(prefix, StringComparison.Ordinal)) {
            return s.Substring(prefix.Length);
        }

        return s;
    }
}
