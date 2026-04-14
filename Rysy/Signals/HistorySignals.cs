using Rysy.History;

namespace Rysy.Signals;

/// <summary>
/// Fired whenever a history action gets applied.
/// </summary>
public record struct HistoryActionApplied(HistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a history action gets simulated, to support live previews of unconfirmed actions.
/// </summary>
public record struct HistoryActionSimulationApplied(HistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a history action gets undone.
/// </summary>
public record struct HistoryActionUndone(HistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a simulated history action gets undone.
/// </summary>
public record struct HistoryActionSimulationUndone(HistoryHandler Handler, IHistoryAction Action) : ISignal;