namespace Rysy.Signals;

/// <summary>
/// Send this signal to queue an action to be performed at the end of the current frame.
/// </summary>
public record struct RunAtEndOfThisFrame(Action Action) : ISignal;
