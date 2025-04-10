using Rysy.Extensions;
using Rysy.Mods;
using Rysy.Platforms;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rysy;

public static class Logger {
    private const string LogFile = "log.txt";
    private const string LastLogFile = "prev-log.txt";

    private static readonly Lock FileLock = new();

    /// <summary>
    /// The path where Rysy was compiled from. Unlike most paths, it's not unbackslashed, to be able to call .TrimStart with it directly.
    /// </summary>
    private static string CompilePath = "";

    public static bool UseColorsInConsole { get; set; } = false;

    public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    private static bool _initialized = false;
    
    private static bool Init() {
        if (!_initialized) {
            _initialized = true;
            DoInit();
        }

        return true;
        
        void DoInit([CallerFilePath] string filePath = "") {
            CompilePath = (Path.GetDirectoryName(filePath) ?? "") + Path.DirectorySeparatorChar;

            var fs = RysyPlatform.Current.GetRysyAppDataFilesystem(profile: null);
            fs.CopyFileTo(LogFile, LastLogFile);
            fs.TryDeleteFile(LogFile);
        }
    }

    private static string PrependLocation(string txt, string callerMethod, string callerFile, int lineNumber)
        => $"[{FancyTextHelper.Gray}{callerFile.TrimStart(CompilePath).Unbackslash()}:{callerMethod}:{lineNumber}{FancyTextHelper.ResetColor}] {txt}";

    private static bool ValidLogLevel(LogLevel level) => Init() && level >= MinimumLevel;

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


        var exString = exception switch {
            _ => exception.ToString()
        };

        var txt = $"[{LogLevel.Error.ToColoredString()}] {message.GetFormattedText()}: {exString}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
    }
    
    public static void Error(string tag, Exception exception, FancyInterpolatedStringHandler message,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(LogLevel.Error))
            return;


        var exString = exception switch {
            _ => exception.ToString()
        };

        var txt = $"[{FancyTextHelper.GetColoredString(tag, 0)}] [{LogLevel.Error.ToColoredString()}] {message.GetFormattedText()}: {exString}\n";

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
        var unformatted = str.UnformatColors();

        var fs = RysyPlatform.Current.GetRysyAppDataFilesystem(null);
        
        lock (FileLock) {
            if (UseColorsInConsole) {
                Console.Write(str.Censor());
                fs.AppendAllText(LogFile, unformatted.Censor());
            } else {
                var censored = unformatted.Censor();
                Console.Write(censored);
                fs.AppendAllText(LogFile, censored);
            }
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
            LogLevel.Info => "\e[96mInfo\e[0m",
            LogLevel.Warning => "\e[93mWarning\e[0m",
            LogLevel.Error => "\e[91mError\e[0m",
            _ => "unknown",
        };

    public static NumVector4 ToColorNumVec(this LogLevel logLevel) => (logLevel switch {
        LogLevel.Debug => Color.LightGray,
        LogLevel.Info => Color.White,
        LogLevel.Warning => Color.Yellow,
        LogLevel.Error => Color.Red,
        _ => Color.White,
    }).ToNumVec4();
}

internal static class FancyTextHelper {
    public const string ResetColor = "\e[0m";

    private static readonly string[] Colors = [
        "\e[96m", // bright cyan
        "\e[92m", // bright green
        "\e[95m", // bright magenta
        //"\e[94m", // bright blue
        "\e[93m", // bright yellow
        "\e[91m", // bright Red
        "\e[31m", // red
        "\e[32m", // green
        "\e[33m", // yellow
        "\e[34m", // blue
        "\e[35m", // magenta
        "\e[36m" // cyan
    ];

    public const string Gray = "\e[90m";

    public static string GetColorCode(int i) => Colors[i % Colors.Length];

    public static string GetColoredString(string from, int colorId) {
        return $"{GetColorCode(colorId)}{from}{ResetColor}";
    }

    public static void AppendFancyText(StringBuilder builder, string? text, int colorId) {
        builder.Append(GetColorCode(colorId));
        builder.Append(text ?? "null");
        builder.Append(ResetColor);
    }
}

[InterpolatedStringHandler]
public ref partial struct FancyInterpolatedStringHandler {
    private readonly StringBuilder _builder;
    private int _colorId = 0;

    public FancyInterpolatedStringHandler(int literalLength, int formattedCount)
        => _builder = new(literalLength);

    public void AppendLiteral(string s) => _builder.Append(s);

    public void AppendFormatted<T>(T t) {
        FancyTextHelper.AppendFancyText(_builder, t?.ToString(), _colorId++);
    }

    public void AppendFormatted<T>(T t, string? format) {
        string? text;
        if (t is IFormattable) {
            text = ((IFormattable) t).ToString(format, /*_provider*/ null); // constrained call to avoid boxing value types
        } else {
            text = t?.ToString();
        }
        FancyTextHelper.AppendFancyText(_builder, text, _colorId++);
    }

    internal string GetFormattedText() => _builder.ToString();

}