namespace Rysy.Helpers;

public sealed class DelayedTaskHelper<TKey> : IDisposable where TKey : notnull {
    private readonly Dictionary<TKey, DateTime> _watcherLastEventTimes = [];
    private readonly Lock _watcherLastEventTimeLock = new();
    private readonly CancellationTokenSource _cts = new();
    
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(0.6);
    
    /// <summary>
    /// Invoked whenever the delay for a given key elapses.
    /// </summary>
    public required Action<TKey> OnDelayElapsed { get; init; }

    public void Register(TKey key) {
        lock (_watcherLastEventTimeLock) {
            if (_watcherLastEventTimes.TryAdd(key, DateTime.Now)) {
                // New Event
                Task.Run(async () => {
                    while (true) {
                        await Task.Delay(Delay);
                        lock (_watcherLastEventTimeLock) {
                            var lastAccessTime = _watcherLastEventTimes[key];
                            if (DateTime.Now - lastAccessTime > Delay - TimeSpan.FromSeconds(0.05)) {
                                _watcherLastEventTimes.Remove(key, out _);
                                break;
                            }
                        }
                    }

                    OnDelayElapsed(key);
                }, _cts.Token);
            } else {
                _watcherLastEventTimes[key] = DateTime.Now;
            }
        }
    }

    public void Dispose() {
        _cts.Dispose();
    }
}