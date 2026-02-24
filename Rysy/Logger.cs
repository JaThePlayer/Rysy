using Rysy.Mods;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LogLevel = Rysy.Shared.LogLevel;

namespace Rysy;

public sealed class Logger(string tag) : IRysyLogger {
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

    internal static bool ValidLogLevel(LogLevel level) => Init() && level >= MinimumLevel;

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

    private static string NotVirtualExceptionToString(Exception ex, string? customMessage)
    {
        // Taken from Exception.ToString()
        const string InnerExceptionPrefix = " ---> ";
        
        string className = ex.GetType().Name;
        string? message = customMessage ?? ex.Message;
        string innerExceptionString = ex.InnerException?.ToString() ?? "";
        string endOfInnerExceptionResource = "   --- End of inner exception stack trace ---";//SR.Exception_EndOfInnerExceptionStack;
        string? stackTrace = ex.StackTrace;

        // Calculate result string length
        int length = className.Length;
        checked
        {
            if (!string.IsNullOrEmpty(message))
            {
                length += 2 + message.Length;
            }
            if (ex.InnerException != null)
            {
                length += Environment.NewLine.Length + InnerExceptionPrefix.Length + innerExceptionString.Length + Environment.NewLine.Length + 3 + endOfInnerExceptionResource.Length;
            }
            if (stackTrace != null)
            {
                length += Environment.NewLine.Length + stackTrace.Length;
            }
        }

        return string.Create(length, (className, message, innerExceptionString, endOfInnerExceptionResource, stackTrace), static (resultSpan, data) => {
            Write(data.className, ref resultSpan);
            if (!string.IsNullOrEmpty(data.message)) {
                Write(": ", ref resultSpan);
                Write(data.message, ref resultSpan);
            }

            if (!string.IsNullOrWhiteSpace(data.innerExceptionString)) {
                Write(Environment.NewLine, ref resultSpan);
                Write(InnerExceptionPrefix, ref resultSpan);
                Write(data.innerExceptionString, ref resultSpan);
                Write(Environment.NewLine, ref resultSpan);
                Write("   ", ref resultSpan);
                Write(data.endOfInnerExceptionResource, ref resultSpan);
            }

            if (data.stackTrace != null) {
                Write(Environment.NewLine, ref resultSpan);
                Write(data.stackTrace, ref resultSpan);
            }
        });

        static void Write(string source, ref Span<char> dest)
        {
            source.CopyTo(dest);
            dest = dest[source.Length..];
        }
    }
    
    private static string ExceptionToString(Exception ex) {
        switch (ex) {
            case YamlDotNet.Core.YamlException yamlException:
                // YamlException hides the type name and stack trace in ToString... thanks
                return NotVirtualExceptionToString(ex, 
                    $"({yamlException.Start}) - ({yamlException.End}): {yamlException.Message}");
            default:
                return ex.ToString();
        }
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

        var txt = $"[{LogLevel.Error.ToColoredString()}] {message}: {ExceptionToString(exception)}\n";

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

        var txt = $"[{LogLevel.Error.ToColoredString()}] {message.GetFormattedText()}: {ExceptionToString(exception)}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
    }
    
    public static void Error(string tag, Exception exception, string message,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!ValidLogLevel(LogLevel.Error))
            return;

        var txt = $"[{FancyTextHelper.GetColoredString(tag, 0)}] [{LogLevel.Error.ToColoredString()}] {message}: {ExceptionToString(exception)}\n";

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

        var txt = $"[{FancyTextHelper.GetColoredString(tag, 0)}] [{LogLevel.Error.ToColoredString()}] {message.GetFormattedText()}: {ExceptionToString(exception)}\n";

#if DEBUG
        txt = PrependLocation(txt, callerMethod, callerFile, lineNumber);
#endif

        WriteImpl(txt);
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

    public bool CanLog(LogLevel level) {
        return ValidLogLevel(level);
    }

    public void Log(LogLevel logLevel, string message) {
        Write(tag, logLevel, message);
    }
    
    public void Log(LogLevel logLevel, FancyInterpolatedStringHandler message) {
        Write(tag, logLevel, message);
    }

    public void Error(Exception ex, string message) {
        Error(tag, ex, message);
    }

    public void Error(Exception ex, FancyInterpolatedStringHandler message) {
        Error(tag, ex, message);
    }
}

public sealed class LoggerFactory : IRysyLoggerFactory {
    public IRysyLogger CreateLogger(string name) {
        return new Logger(name);
    }
}

public static class LogLevelExtensions {
    /// <summary>
    /// Writes this object to the log as JSON.
    /// </summary>
    public static void LogAsJson<T>(this T? obj, string tag = "LogAsJson", LogLevel level = LogLevel.Debug, [CallerArgumentExpression(nameof(obj))] string caller = "", 
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        if (!Logger.ValidLogLevel(level))
            return;

        if (obj is null) {
#if DEBUG
            Logger.Write(tag, level, "null", callerMethod, callerFile, lineNumber);
#else
            Logger.Write(tag, level, "null");
#endif
            return;
        }

        FancyInterpolatedStringHandler txt = $"{caller} = {obj.ToJson()}";
#if DEBUG
        Logger.Write(tag, level, txt, callerMethod, callerFile, lineNumber);
#else
        Logger.Write(tag, level, txt);
#endif
    }
    
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
