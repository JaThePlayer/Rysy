using Rysy.Extensions;
using Rysy.Platforms;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Rysy;

public static class Logger {
    private static string LogFile = $"{RysyPlatform.Current.GetSaveLocation()}/log.txt";
    private static string LastLogFile = $"{RysyPlatform.Current.GetSaveLocation()}/prev-log.txt";

    private static object FILE_LOCK = new();

    /// <summary>
    /// The path where Rysy was compiled from. Unlike most paths, it's not unbackslashed, to be able to call .TrimStart with it directly.
    /// </summary>
    private static string CompilePath = "";

    public static bool UseColorsInConsole { get; set; } = false;

    public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    public static void Init([CallerFilePath] string filePath = "") {
        CompilePath = (Path.GetDirectoryName(filePath) ?? "") + Path.DirectorySeparatorChar;

        if (File.Exists(LogFile)) {
            File.Copy(LogFile, LastLogFile, true);
            File.Delete(LogFile);
        }
    }

    private static string PrependLocation(string txt, string callerMethod, string callerFile, int lineNumber)
        => $"[{FancyTextHelper.Gray}{callerFile.TrimStart(CompilePath).Unbackslash()}:{callerMethod}:{lineNumber}{FancyTextHelper.RESET_COLOR}] {txt}";

    private static bool ValidLogLevel(LogLevel level) => level >= MinimumLevel;

    public static void Write(string tag, LogLevel logLevel, string msg, 
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(logLevel))
            return;

        var txt = $"[{FancyTextHelper.GetColoredString(tag, 0)}] [{logLevel.ToColoredString()}] {msg}\n";
#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif
        WriteImpl(txt);
    }

    public static void Write(string tag, LogLevel logLevel, FancyInterpolatedStringHandler msg, 
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(logLevel))
            return;

        var txt = $"[{FancyTextHelper.GetColoredString(tag, 0)}] [{logLevel.ToColoredString()}] {msg.GetFormattedText()}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
    }

    public static void Error(Exception exception, string message,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(LogLevel.Error))
            return;

        var txt = $"[{LogLevel.Error.ToColoredString()}] {message}: {exception.ToString()}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
    }

    public static void Error(Exception exception, FancyInterpolatedStringHandler message,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(LogLevel.Error))
            return;

        var txt = $"[{LogLevel.Error.ToColoredString()}] {message.GetFormattedText()}: {exception.ToString()}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
    }

    /// <summary>
    /// Writes this object to the log as JSON.
    /// </summary>
    public static void LogAsJson<T>(this T? obj, string tag = "LogAsJson", LogLevel level = LogLevel.Debug, [CallerArgumentExpression(nameof(obj))] string caller = "", 
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(level))
            return;

        if (obj is null) {
#if DEBUG
            Write(tag, level, "null", callerMethod, callerFile, lineNumber);
#else
            Write(tag, level, "null");
#endif
            return;
        }

        FancyInterpolatedStringHandler txt = $"{caller} = {obj.ToJson()}";
#if DEBUG
        Write(tag, level, txt, callerMethod, callerFile, lineNumber);
#else
        Write(tag, level, txt);
#endif
    }

    private static void WriteImpl(string str) {
        lock (FILE_LOCK) {
            var unformatted = str.UnformatColors();
            if (UseColorsInConsole) {
                Console.Write(str.Censor());
            } else {
                Console.Write(unformatted.Censor());
            }

            File.AppendAllText(LogFile, unformatted); 
        }

    }
}

public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
}

public static class LogLevelExtensions {
    public static string FastToString(this LogLevel logLevel)
        => logLevel switch {
            LogLevel.Debug => "Debug",
            LogLevel.Info => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            _ => "unknown",
        };

    public static string ToColoredString(this LogLevel logLevel)
        => logLevel switch {
            LogLevel.Debug => "Debug",
            LogLevel.Info => "\u001b[96mInfo\u001b[0m",
            LogLevel.Warning => "\u001b[93mWarning\u001b[0m",
            LogLevel.Error => "\u001b[91mError\u001b[0m",
            _ => "unknown",
        };
}

internal static class FancyTextHelper {
    public const string RESET_COLOR = "\u001b[0m";

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

    public const string Gray = "\u001b[90m";

    public static string GetColorCode(int i) => Colors[i % Colors.Length];

    public static string GetColoredString(string from, int colorId) {
        return $"{GetColorCode(colorId++)}{from}{RESET_COLOR}";
    }

    public static void AppendFancyText(StringBuilder builder, string? text, int colorId) {
        builder.Append(GetColorCode(colorId++));
        builder.Append(text ?? "null");
        builder.Append(RESET_COLOR);
    }
}

[InterpolatedStringHandler]
public ref partial struct FancyInterpolatedStringHandler {
    readonly StringBuilder builder;
    int colorId = 0;

    public FancyInterpolatedStringHandler(int literalLength, int formattedCount)
        => builder = new(literalLength);

    public void AppendLiteral(string s) => builder.Append(s);

    public void AppendFormatted<T>(T t) {
        FancyTextHelper.AppendFancyText(builder, t?.ToString(), colorId++);
    }

    public void AppendFormatted<T>(T t, string? format) {
        string? text;
        if (t is IFormattable) {
            text = ((IFormattable) t).ToString(format, /*_provider*/ null); // constrained call to avoid boxing value types
        } else {
            text = t?.ToString();
        }
        FancyTextHelper.AppendFancyText(builder, text, colorId++);
    }

    internal string GetFormattedText() => builder.ToString();

}