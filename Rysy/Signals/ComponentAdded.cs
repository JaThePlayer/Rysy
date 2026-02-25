namespace Rysy.Signals;

public record struct ComponentAdded<T>(T Component) : ISignal;