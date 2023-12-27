namespace Rysy.Loading;

public sealed record LoadTaskResult(Exception? Exception) {

    public static LoadTaskResult Success() => new((Exception?)null);
    
    public static LoadTaskResult Error(Exception ex) => new(ex);
}

public abstract class LoadTask {
    private Task<LoadTaskResult>? _task;
    
    public abstract string Name { get; }
    
    public abstract List<string> CurrentMessages { get; }
    
    protected abstract Task<LoadTaskResult> DoRun();

    public bool Running => _task is { IsCompleted: false };
    
    public Task<LoadTaskResult> Run() {
        _task = DoRun();

        return _task;
    }
}

public sealed class SimpleLoadTask(string name, Func<SimpleLoadTask, Task<LoadTaskResult>> run) : LoadTask {
    public override string Name => name;

    private List<string> _messages = [];

    public override List<string> CurrentMessages => _messages.ToList();
    
    protected override async Task<LoadTaskResult> DoRun() {
        var ret = await run(this);
        SetMessage();
        return ret;
    }

    public void SetMessage(params string[] newMsg) {
        _messages = newMsg.ToList();
    }

    public void SetMessage(int index, string msg) {
        if (_messages.Count <= index) {
            _messages.Add(msg);
        } else {
            _messages[index] = msg;
        }
    }
}

public sealed class ParallelLoadTask(string name, SimpleLoadTask[] tasks) : LoadTask {
    public override string Name => name;
    
    public override List<string> CurrentMessages => tasks.SelectMany(t => {
        var name = t.Name;
        var msg = t.CurrentMessages;

        if (msg.Count == 0)
            return [];
        
        return msg.Prepend(name);
    }).ToList();
    
    protected override async Task<LoadTaskResult> DoRun() {
        var results = await Task.WhenAll(tasks.Select(t => t.Run()));

        var firstError = results.FirstOrDefault(r => r.Exception is { });
        return firstError ?? LoadTaskResult.Success();
    }
}

public sealed class LoadTaskManager(List<LoadTask> tasks) {
    private object _lock = new();
    
    private LoadTask? _current;
    
    public async Task<Exception?> LoadAll() {
        foreach (var t in tasks) {
            lock (_lock)
                _current = t;
            var res = await t.Run();

            if (res.Exception is { } ex) {
                return ex;
            }
        }

        _current = null;
        return null;
    }

    public List<string> GetCurrentMessages() {
        lock (_lock) {
            if (_current is { }) {
                var list = _current.CurrentMessages.ToList();
                list.Insert(0, _current.Name);
            
                return list;
            } else {
                return [];
            }
        }
    }
}