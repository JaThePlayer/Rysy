using Rysy.Components;

namespace Rysy.Signals;

/// <summary>
/// Allows the implementing component to easily send signals to some target, provided externally.
/// </summary>
public interface ISignalEmitter {
    public SignalTarget SignalTarget { get; set; }
}

public static class SignalEmitterExt {
    extension(ISignalEmitter emitter) {
        public void Emit<T>(T signal) where T : ISignal {
            emitter.SignalTarget.Send(signal);
        }
    }
}

public readonly struct SignalTarget {
    private readonly WeakReference<ISignalListener>? _listener;
    
    private SignalTarget(ISignalListener? listener) {
        _listener = listener is {} ? new(listener) : null;
    }

    public static SignalTarget From(ISignalListener listener) {
        return new SignalTarget(listener);
    }

    public static SignalTarget Null { get; } = new(null);
    
    public void Send<T>(T signal) where T : ISignal {
        if (_listener?.TryGetTarget(out var listener) ?? false)
            listener.OnSignal(signal);
    }
}