using Rysy.Shared;
using System;
using RysyLogLevel = Rysy.Shared.LogLevel;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class CelesteRysyLogger(string tag) : IRysyLogger {
    public bool CanLog(RysyLogLevel level) {
        return ToCelesteLevel(level) >= Logger.GetLogLevel(tag);
    }

    public void Log(RysyLogLevel logLevel, string message) {
        Logger.Log(ToCelesteLevel(logLevel), tag, message);
    }
    
    public void Log(RysyLogLevel logLevel, FancyInterpolatedStringHandler message) {
        Logger.Log(ToCelesteLevel(logLevel), tag, message.GetFormattedText());
    }

    public void Error(Exception ex, string message) {
        Logger.Error(tag, $"{message}: {ex}");
    }
    
    public void Error(Exception ex, FancyInterpolatedStringHandler message) {
        Logger.Error(tag, $"{message.GetFormattedText()}: {ex}");
    }

    private static LogLevel ToCelesteLevel(RysyLogLevel level) =>
        level switch {
            RysyLogLevel.Debug => LogLevel.Debug,
            RysyLogLevel.Info => LogLevel.Info,
            RysyLogLevel.Warning => LogLevel.Warn,
            RysyLogLevel.Error => LogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
}