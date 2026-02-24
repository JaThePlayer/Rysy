namespace Rysy.Signals;

/// <summary>
/// Sent whenever Rysy's settings get changed.
/// </summary>
/// <param name="Settings">The instance that got changed.</param>
/// <param name="SettingName">The name of the setting changed, use nameof for comparisons.</param>
/// <param name="OldValue">Old value of the setting.</param>
/// <param name="Value">New value of the setting.</param>
/// <typeparam name="T">The type of the field that got changed.</typeparam>
public record struct SettingsChanged<T>(Settings Settings, string SettingName, T? OldValue, T Value) : ISignal;

public record struct SettingsChanged(Settings Settings, string SettingName) : ISignal;
