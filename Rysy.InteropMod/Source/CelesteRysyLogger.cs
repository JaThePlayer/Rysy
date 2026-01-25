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

    private static LogLevel ToCelesteLevel(RysyLogLevel level) =>
        level switch {
            RysyLogLevel.Debug => LogLevel.Debug,
            RysyLogLevel.Info => LogLevel.Info,
            RysyLogLevel.Warning => LogLevel.Warn,
            RysyLogLevel.Error => LogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
}