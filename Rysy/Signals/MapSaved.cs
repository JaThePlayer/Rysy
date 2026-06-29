namespace Rysy.Signals;

/// <summary>
/// Fired whenever the currently edited map gets saved.
/// </summary>
public record struct MapSaved(Map Map) : ISignal;
