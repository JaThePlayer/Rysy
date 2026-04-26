using System.IO.Pipes;
using System.Text.Json;

namespace Rysy.Shared.Networking;

public sealed class InPipeServer<T>(IRysyLogger logger) : IDisposable {
    public string? Name { get; set; }

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = NetworkingJsonOptions.IncludeFields;
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private NamedPipeClientStream? _pipe;
    
    private StreamReader? _writer;
    
    public required Action<T> OnMessageReceived { get; init; }
    
    private async Task RunServerAsync(CancellationToken ct) {
        await Restart();

        while (!ct.IsCancellationRequested) {
            string? line;
            try {
                line = await _writer.ReadLineAsync(ct);
            } catch (IOException e) {
                await Restart();
                continue;
            } catch (OperationCanceledException) {
                break;
            }
            
            if (line is null) {
                await Restart();
                continue;
            }
            
            try {
                logger.Info($"Received: {line}");
                var d = JsonSerializer.Deserialize<T>(line, JsonSerializerOptions);
                if (d is {})
                    OnMessageReceived(d);
            } catch (Exception e) {
                logger.Error($"Error while parsing '{Name}' from pipe.\n{e}");
                break;
            }
        }
        
        Dispose();
        return;
        
        async Task Restart() {
            _pipe?.Close();
            _pipe = CreatePipe();
            logger.Info($"Awaiting for a '{Name}' pipe.");
        
            await _pipe.ConnectAsync(ct);
        
            logger.Info($"Connected to '{Name}' pipe.");
            _writer = new StreamReader(_pipe);
        }
    }

    public void Load() {
        Task.Run(() => RunServerAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    private NamedPipeClientStream CreatePipe()
    {
        return new NamedPipeClientStream(".",
            Name ??= $"Celeste-Rysy-Pipe-Server-{typeof(T).FullName}",
            PipeDirection.In,
            PipeOptions.Asynchronous);
    }

    public void Dispose() {
        if (_pipe is { IsConnected: true })
            logger.Info($"Disposing a '{Name}' pipe.");
        _cancellationTokenSource.Dispose();
        _pipe?.Dispose();
        _writer?.Dispose();
    }
}