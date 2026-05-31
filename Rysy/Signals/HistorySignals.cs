using Rysy.History;

namespace Rysy.Signals;

/// <summary>
/// Fired whenever a history action gets applied.
/// </summary>
public record struct HistoryActionApplied(IHistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a history action gets simulated, to support live previews of unconfirmed actions.
/// </summary>
public record struct HistoryActionSimulationApplied(IHistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a history action gets undone.
/// </summary>
public record struct HistoryActionUndone(IHistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever a simulated history action gets undone.
/// </summary>
public record struct HistoryActionSimulationUndone(IHistoryHandler Handler, IHistoryAction Action) : ISignal;

/// <summary>
/// Fired whenever any history change happens, including both applied and undone actions.
/// Useful for things like refreshing the undo/redo buttons in the UI.
/// </summary>
public record struct HistoryChanged(IHistoryHandler Handler) : ISignal;
