using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rysy;

public static class Logger
{
    public static void Write(string tag, LogLevel logLevel, string msg)
    {
        Console.WriteLine($"[{FancyTextHelper.GetColoredString(tag, 0)}] [{logLevel.ToColoredString()}] {msg}");
    }

    public static void Write(string tag, LogLevel logLevel, FancyInterpolatedStringHandler msg)
    {
        Console.WriteLine($"[{FancyTextHelper.GetColoredString(tag, 0)}] [{logLevel.ToColoredString()}] {msg.GetFormattedText()}");
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

public static class LogLevelExtensions
{
    public static string FastToString(this LogLevel logLevel)
        => logLevel switch
        {
            LogLevel.Debug => "Debug",
            LogLevel.Info => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            _ => "unknown",
        };

    public static string ToColoredString(this LogLevel logLevel)
        => logLevel switch
        {
            LogLevel.Debug => "Debug",
            LogLevel.Info => "\u001b[96mInfo\u001b[0m",
            LogLevel.Warning => "\u001b[93mWarning\u001b[0m",
            LogLevel.Error => "\u001b[91mError\u001b[0m",
            _ => "unknown",
        };
}

internal static class FancyTextHelper
{
    public const string RESET_COLOR = "\u001b[0m";

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        //IgnoreReadOnlyProperties = true,
    };

    public static readonly string[] Colors = new[] {
        "\u001b[96m", // bright cyan
        "\u001b[92m", // bright green
        "\u001b[95m", // bright magenta
        //"\u001b[94m", // bright blue
        "\u001b[93m", // bright yellow
        "\u001b[91m", // bright Red
        "\u001b[31m", // red
        "\u001b[32m", // green
        "\u001b[33m", // yellow
        "\u001b[34m", // blue
        "\u001b[35m", // magenta
        "\u001b[36m", // cyan
    };

    public static string GetColorCode(int i) => Colors[i % Colors.Length];

    public static string GetColoredString(string from, int colorId)
    {
        return $"{GetColorCode(colorId++)}{from}{RESET_COLOR}";
    }

    public static void AppendFancyText(StringBuilder builder, string? text, int colorId)
    {
        builder.Append(GetColorCode(colorId++));
        builder.Append(text ?? "null");
        builder.Append(RESET_COLOR);
    }
}

[InterpolatedStringHandler]
public ref partial struct FancyInterpolatedStringHandler
{
    readonly StringBuilder builder;
    int colorId = 0;

    public FancyInterpolatedStringHandler(int literalLength, int formattedCount)
        => builder = new(literalLength);

    public void AppendLiteral(string s) => builder.Append(s);

    public void AppendFormatted<T>(T t)
    {
        FancyTextHelper.AppendFancyText(builder, t?.ToString(), colorId++);
    }

    public void AppendFormatted<T>(T t, string? format)
    {
        string? text;
        if (t is IFormattable)
        {
            text = ((IFormattable)t).ToString(format, /*_provider*/ null); // constrained call to avoid boxing value types
        } else
        {
            text = t?.ToString();
        }
        FancyTextHelper.AppendFancyText(builder, text, colorId++);
    }

    internal string GetFormattedText() => builder.ToString();
    internal string GetUnformattedText() => builder.ToString().RegexReplace(UnformatRegex(), "");
    [GeneratedRegex("\u001b\\[.{1,2}m")]
    private static partial Regex UnformatRegex();
}