using Rysy.Signals;

namespace Rysy.Components;

/// <summary>
/// Allows the given component to listen to all signals.
/// </summary>
public interface ISignalListener {
    public void OnSignal<T>(T signal) where T : ISignal;
}

/// <summary>
/// Allows the given component to listen to a specific type of signal.
/// </summary>
public interface ISignalListener<T> where T : ISignal {
    public void OnSignal(T signal);
}

public static class SignalUtils {
    public static void SendTo<T>(object obj, T signal) where T : ISignal {
        if (obj is ISignalListener listener) {
            listener.OnSignal(signal);
        }

        if (obj is ISignalListener<T> specificListener) {
            specificListener.OnSignal(signal);
        }
    }
}