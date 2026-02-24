namespace Rysy.Signals;

/// <summary>
/// Sent whenever Rysy's persistence get changed.
/// </summary>
/// <param name="Persistence">The instance that got changed.</param>
/// <param name="SettingName">The name of the setting changed, use nameof for comparisons.</param>
/// <param name="OldValue">Old value of the setting.</param>
/// <param name="Value">New value of the setting.</param>
/// <typeparam name="T">The type of the field that got changed.</typeparam>
public record struct PersistenceChanged<T>(Persistence Persistence, string SettingName, T? OldValue, T Value) : ISignal;

public record struct PersistenceChanged(Persistence Persistence, string SettingName) : ISignal;
