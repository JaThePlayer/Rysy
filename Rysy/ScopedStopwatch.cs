using Rysy;
using System.Diagnostics;

/// <summary>
/// A class that runs a stopwatch, and stops it when it gets disposed, logging how much time elapsed
/// </summary>
public sealed class ScopedStopwatch : IDisposable
{
    public Stopwatch Watch;
    public string Message;

    public ScopedStopwatch(string msg)
    {
        Message = msg;
        Watch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        Watch.Stop();

        Logger.Write("ScopedStopwatch", LogLevel.Debug, $"{Message}: {Watch.Elapsed.TotalMilliseconds}ms");
    }
}