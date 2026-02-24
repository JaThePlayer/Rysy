using System.Runtime.CompilerServices;
using System.Text;

namespace Rysy.Shared;

public interface IRysyLogger {
    public bool CanLog(LogLevel level);
    
    public void Log(LogLevel logLevel, string message);
    
    public void Log(LogLevel logLevel, FancyInterpolatedStringHandler message);

    public void Error(Exception ex, string message);
    
    public void Error(Exception ex, FancyInterpolatedStringHandler message);
}

public interface IRysyLogger<T> : IRysyLogger;

public sealed class Logger<T>(IRysyLoggerFactory factory) : IRysyLogger<T> {
    private readonly IRysyLogger _logger = factory.CreateLogger(typeof(T).Name);

    public bool CanLog(LogLevel level) {
        return _logger.CanLog(level);
    }

    public void Log(LogLevel logLevel, string message) {
        _logger.Log(logLevel, message);
    }

    public void Log(LogLevel logLevel, FancyInterpolatedStringHandler message) {
        _logger.Log(logLevel, message);
    }

    public void Error(Exception ex, string message) {
        _logger.Error(ex, message);
    }

    public void Error(Exception ex, FancyInterpolatedStringHandler message) {
        _logger.Error(ex, message);
    }
}

public interface IRysyLoggerFactory {
    public IRysyLogger CreateLogger(string name);
    
    public IRysyLogger<T> CreateLogger<T>() => new Logger<T>(this);
}

public static class RysyLoggerExt {
    extension(IRysyLogger logger) {
        public void Warn(string message) {
            logger.Log(LogLevel.Warning, message);
        }
        
        public void Warn(FancyInterpolatedStringHandler message) {
            logger.Log(LogLevel.Warning, message);
        }
        
        public void Error(string message) {
            logger.Log(LogLevel.Error, message);
        }
        
        public void Error(FancyInterpolatedStringHandler message) {
            logger.Log(LogLevel.Error, message);
        }

        public void Info(string message) {
            logger.Log(LogLevel.Info, message);
        }
        
        public void Info(FancyInterpolatedStringHandler message) {
            logger.Log(LogLevel.Info, message);
        }
    }

    extension(IRysyLoggerFactory factory) {
        public IRysyLogger CreateLogger(Type t) => factory.CreateLogger(t.Name);
    }
}

public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
}

public static class FancyTextHelper {
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
public ref struct FancyInterpolatedStringHandler {
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

    public string GetFormattedText() => _builder.ToString();
}