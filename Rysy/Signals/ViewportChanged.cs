namespace Rysy.Signals;

public record ViewportChanged(Viewport Viewport) : ISignal;