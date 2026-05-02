namespace Rysy.Signals;

public record struct ComponentAdded<T>(T Component) : ISignal;

public record struct ComponentAdded(object Component) : ISignal;

public record struct ComponentRemoved(object Component) : ISignal;
