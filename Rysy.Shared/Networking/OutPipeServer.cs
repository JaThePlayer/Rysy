using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;

namespace Rysy.Shared.Networking;

/// <summary>
/// Pipeline server, which can be used to send objects to a different .NET process.
/// </summary>
/// <typeparam name="T">The type to be sent.</typeparam>
public sealed class OutPipeServer<T>(IRysyLogger logger) : IDisposable {
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private NamedPipeServerStream? _pipe;
    
    private StreamWriter? _writer;
    
    private readonly BlockingCollection<T> _messageQueue = new(boundedCapacity: 10);

    private async Task RunServerAsync(CancellationToken ct) {
        await Restart();

        while (!ct.IsCancellationRequested) {
            if (_messageQueue.TryTake(out var message, TimeSpan.FromSeconds(1))) {
                try {
                    await _writer.WriteLineAsync(JsonSerializer.Serialize(message, NetworkingJsonOptions.IncludeFields));

                    if (message is IDisposable d) {
                        d.Dispose();
                    }
                } catch (IOException e) {
                    logger.Error($"Error while sending message to stream: {e}");
                    await Restart();
                }
            }
        }

        async Task Restart() {
            if (_pipe is not null)
                await _pipe.DisposeAsync();
            
            _pipe = CreatePipe();
            logger.Warn($"Awaiting for a {typeof(T).FullName} pipe connection.");
            await _pipe.WaitForConnectionAsync(ct);
            _writer = new StreamWriter(_pipe) { AutoFlush = true };
            logger.Info("Connected!");
        }
    }

    public void Enqueue(T data) {
        _messageQueue.TryAdd(data, TimeSpan.FromSeconds(0.33));
    }

    public bool IsConnected => _pipe?.IsConnected ?? false;

    public void Load() {
        Task.Run(() => RunServerAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    private static NamedPipeServerStream CreatePipe()
    {
        return new NamedPipeServerStream(
            $"Celeste-Rysy-Pipe-Server-{typeof(T).FullName}",
            PipeDirection.Out,
            maxNumberOfServerInstances: 2,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }


    public void Dispose() {
        _cancellationTokenSource.Dispose();
        _pipe?.Dispose();
        _writer?.Dispose();
        _messageQueue.Dispose();
    }
}