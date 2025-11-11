using Rysy;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rysy;

/// <summary>
/// A class that runs a stopwatch, and stops it when it gets disposed, logging how much time elapsed
/// </summary>
public sealed class ScopedStopwatch : IDisposable {
    public Stopwatch Watch;
    public string Message;

    private string _callerMethod, _callerFile;
    private int _callerLineNumber;

    public ScopedStopwatch(string msg, 
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        Message = msg;
        Watch = Stopwatch.StartNew();

        (_callerMethod, _callerFile, _callerLineNumber) = (callerMethod, callerFile, lineNumber);
    }

    public void Dispose() {
        Watch.Stop();

        Logger.Write("ScopedStopwatch", LogLevel.Debug, $"{Message}: {Watch.Elapsed.TotalMilliseconds}ms", _callerMethod, _callerFile, _callerLineNumber);
    }
}