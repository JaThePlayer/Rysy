using Rysy.Extensions;
using Rysy.Helpers;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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

    extension(string pascalCase)
    {
        /// <summary>
        /// Splits the string on [a-z][A-Z] patterns, inserting a space between them.
        /// </summary>
        public string SplitPascalCase() {
            return PascalCaseRegex.Replace(pascalCase, (match) => {
                return $"{match.ValueSpan[0]} {match.ValueSpan[1]}";
            });
        }

        /// <summary>
        /// Trims a piece of text from the end of the string
        /// </summary>
        [Pure]
        public string TrimEnd(string elem, StringComparison comp = StringComparison.InvariantCulture) {
            if (pascalCase.EndsWith(elem, comp))
                return pascalCase[..^elem.Length];
            return pascalCase;
        }

        /// <summary>
        /// Trims a piece of text from the start of the string
        /// </summary>
        [Pure]
        public string TrimStart(string elem) {
            if (pascalCase.StartsWith(elem, StringComparison.Ordinal))
                return pascalCase[elem.Length..];
            return pascalCase;
        }

        /// <summary>
        /// Replaces backslashes with slashes in the given string
        /// </summary>
        [Pure]
        public string Unbackslash()
            => pascalCase.Replace('\\', '/');

        /// <summary>
        /// Corrects the slashes in the given path to be correct for the given OS.
        /// Since all OS'es seem to support forward slashes, only use this for printing!
        /// </summary>
        public string CorrectSlashes()
            => pascalCase switch {
                null => "",
                _ => pascalCase.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar),
            };

        /// <summary>
        /// Tries to censor personal info from filepaths, specifically windows usernames.
        /// Should be used when a path is displayed on-screen
        /// </summary>
        public string Censor() {
            if (string.IsNullOrWhiteSpace(pascalCase))
                return pascalCase;


            if (UserNameRegex.Matches(pascalCase) is { } matches) {
                for (int i = 0; i < matches.Count; i++)
                    pascalCase = pascalCase.Replace(matches[i].Groups[1].Value, "<USER>", StringComparison.Ordinal);
                return pascalCase;
            }

            return pascalCase;
        }
    }


    /// <param name="path"></param>
    extension(string? path)
    {
        /// <summary>
        /// Calls <see cref="Path.GetDirectoryName(string?)"/> on this string
        /// </summary>
        public string? Directory() {
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Calls <see cref="Path.GetFileNameWithoutExtension(string?)"/> on this string
        /// </summary>
        /// <returns></returns>
        public string? FilenameNoExt() {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Calls <see cref="Path.GetExtension(string?)"/> on this string
        /// </summary>
        public string? FileExtension() {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// Calls <see cref="Path.GetExtension(string?)"/> on this string, returns a string without the dot at the start of the file extension.
        /// </summary>
        public string? FileExtensionNoDot() {
            return Path.GetExtension(path)?.TrimPrefix(".");
        }
    }

    extension(string from)
    {
        /// <summary>
        /// Calls <see cref="Regex.Replace(string, string)"/> with the provided regex with the given strings
        /// </summary>
        public string RegexReplace(Regex regex, string with)
            => regex.Replace(from, with);

        /// <summary>
        /// Makes each word in the given string start with an lowercase char
        /// </summary>
        public string LowercaseFirst() {
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
        public string UppercaseFirst() {
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
    }

    private static readonly char[] HumanizeReplacedChars = new[] { '.', '_'};

    private static readonly ConcurrentDictionary<string, string> HumanizeCache = new();

    /// <summary>
    /// Combines <see cref="SplitPascalCase(string)"/> and <see cref="UppercaseFirst(string)"/>
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Humanize(this string? text) {
        if (text is null)
            return "";

        return HumanizeCache.GetOrAdd(text, static text => text?.TrimStart('_').ReplaceAll(HumanizeReplacedChars, ' ').SplitPascalCase().UppercaseFirst() ?? "");
    }

    extension(string formatted)
    {
        /// <summary>
        /// Returns the string <paramref name="formatted"/> with all color formatting removed.
        /// </summary>
        public string UnformatColors() => formatted.RegexReplace(UnformatRegex, "");

        /// <summary>
        /// Returns <paramref name="formatted"/> suffixed in such a way that the returned string is not contained in <paramref name="strings"/>.
        /// </summary>
        public string GetDeduplicatedIn(IEnumerable<string> strings) {
            int i = 0;
            var origName = formatted;
            var newName = origName;

            var stringsHashSet = strings.ToHashSet(StringComparer.Ordinal);

            while (stringsHashSet.Contains(newName)) {
                newName = $"{origName}-{i}";
                i++;
            }

            return newName;
        }
    }


    extension(string? str)
    {
        /// <summary>
        /// Converts the given string to create a string that's a valid filename, by replacing all illegal characters with an underscore.
        /// </summary>
        public string ToValidFilename() {
            if (str == null)
                return string.Empty;

            return str.ReplaceAll(Path.GetInvalidFileNameChars(), '_');
        }

        /// <summary>
        /// Converts the given string to create a string that's a valid filepath, by replacing all illegal characters with an underscore.
        /// </summary>
        public string ToValidFilePath() {
            if (str == null)
                return string.Empty;

            return str.ReplaceAll(InvalidFilePathChars, '_');
        }
    }

    internal static readonly char[] InvalidFilePathChars = Path.GetInvalidFileNameChars().Except(['/', '\\']).ToArray();

    extension(string str)
    {
        public string ReplaceAll(char[] chars, char replacement) {
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

        public string ToVirtPath(string prefix = "")
            => ToVirtPath(str.AsSpan(), prefix);
    }

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

    extension(string str)
    {
        /// <summary>
        /// Translates the string using <see cref="LangRegistry.Translate(string)"/>.
        /// If no translation is found, returns the string itself.
        /// </summary>
        public string Translate() => LangRegistry.Translate(str);

        /// <summary>
        /// Translates the string using <see cref="LangRegistry.Translate(string)"/>, then formats it using <see cref="string.Format(string, object?[])"/>
        /// If no translation is found, formats the string itself.
        /// </summary>
        public string TranslateFormatted(params object[] args) => string.Format(CultureInfo.CurrentCulture, LangRegistry.Translate(str), args);

        /// <summary>
        /// Translates the string using <see cref="LangRegistry.TranslateOrNull(string)"/>.
        /// If no translation is found, returns null.
        /// </summary>
        public string? TranslateOrNull() => LangRegistry.TranslateOrNull(str);
    }

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

    extension(string str)
    {
        /// <summary>
        /// Translates the string $"{<paramref name="prefix"/>}.{<paramref name="str"/>}" using <see cref="LangRegistry.TranslateOrNull(string)"/>.
        /// If no translation is found, returns the result of calling <see cref="Humanize(string)"/> on <paramref name="str"/>
        /// </summary>
        public string TranslateOrHumanize(string prefix) => TranslateOrNull(str, prefix) ?? Humanize(str);

        public string TranslateOrHumanize(ReadOnlySpan<char> prefix) => TranslateOrNull(str, prefix) ?? Humanize(str);
        public LangKey ToLangKey(params object[] args) => new LangKey(str, args);
        public int ToInt() => Convert.ToInt32(str, CultureInfo.InvariantCulture);
        public int ToIntHex() => int.Parse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        public uint ToUInt() => Convert.ToUInt32(str, CultureInfo.InvariantCulture);
        public uint ToUIntHex() => uint.Parse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        public short ToShort() => Convert.ToInt16(str, CultureInfo.InvariantCulture);
        public ushort ToUShort() => Convert.ToUInt16(str, CultureInfo.InvariantCulture);
        public byte ToByte() => Convert.ToByte(str, CultureInfo.InvariantCulture);
        public sbyte ToSByte() => Convert.ToSByte(str, CultureInfo.InvariantCulture);
        public float ToSingle() => Convert.ToSingle(str, CultureInfo.InvariantCulture);
        public double ToDouble() => Convert.ToDouble(str, CultureInfo.InvariantCulture);
        public decimal ToDecimal() => Convert.ToDecimal(str, CultureInfo.InvariantCulture);
    }

    public static float ToSingle(this ReadOnlySpan<char> s) => float.Parse(s, CultureInfo.InvariantCulture);
    public static float ToSingle(this Span<char> s) => float.Parse(s, CultureInfo.InvariantCulture);
    public static float ToInt(this ReadOnlySpan<char> s) => int.Parse(s, CultureInfo.InvariantCulture);

    public static bool IsNullOrWhitespace([NotNullWhen(false)] this string? s) => string.IsNullOrWhiteSpace(s);

    extension(string s)
    {
        /// <summary>
        /// Counts how many times the character <paramref name="x"/> appears in the given string.
        /// </summary>
        public int CountFast(char x) {
            var sum = 0;
            var i = -1;
            while ((i = s.IndexOf(x, i + 1)) != -1) {
                sum++;
            }
            return sum;
        }

        public string TrimPrefix(ReadOnlySpan<char> prefix) {
            if (s.AsSpan().StartsWith(prefix, StringComparison.Ordinal)) {
                return s[prefix.Length..];
            }

            return s;
        }

        public string TrimPostfix(ReadOnlySpan<char> prefix, StringComparison comparison = StringComparison.Ordinal) {
            if (s.AsSpan().EndsWith(prefix, comparison)) {
                return s[..^prefix.Length];
            }

            return s;
        }

        public string TrimBeyondLength(int length) {
            if (s.Length <= length)
                return s;

            return string.Concat(s.AsSpan(0, length), "(...)");
        }
    }

    public static string ToImguiEscapedString(this char c) => c is '%' ? "%%" : c.ToString();
    
    /// <summary>
    /// If the provided string is null or empty, returns "##", which is safe to use in ImGui.
    /// </summary>
    public static string ToImguiEscaped(this string? str) => str.IsNullOrWhitespace() ? "##" : str;

    public static string ToStringInvariant(this object? obj) {
        if (obj is IFormattable f)
            return f.ToString(null, CultureInfo.InvariantCulture);
        return obj?.ToString() ?? "";
    }

    [Pure]
    public static string AddPrefixIfNeeded(this string? str, string prefix, StringComparison comparisonType = StringComparison.Ordinal) {
        if (str is null)
            return prefix;
        
        if (str.StartsWith(prefix, comparisonType))
            return str;
        return prefix + str;
    }
    
    [Pure]
    public static string AddPostfixIfNeeded(this string str, string postfix, StringComparison comparisonType = StringComparison.Ordinal) {
        if (str.EndsWith(postfix, comparisonType))
            return str;
        return str + postfix;
    }
}
