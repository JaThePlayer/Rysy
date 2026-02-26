namespace Rysy.Signals;

public record struct SelfAdded(IComponentRegistry Registry) : ISignal;

public record struct SelfRemoved() : ISignal;