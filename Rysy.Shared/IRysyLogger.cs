namespace Rysy.Shared;

public interface IRysyLogger {
    public bool CanLog(LogLevel level);
    
    public void Log(LogLevel logLevel, string message);    
}

public static class RysyLoggerExt {
    extension(IRysyLogger logger) {
        public void Warn(string message) {
            logger.Log(LogLevel.Warning, message);
        }
        
        public void Error(string message) {
            logger.Log(LogLevel.Error, message);
        }

        public void Info(string message) {
            logger.Log(LogLevel.Info, message);
        }
    }
}

public enum LogLevel {
    Debug,
    Info,
    Warning,
    Error,
}
