namespace Rysy.Shared.InteropMod;

public sealed class InGameSettings {
    public const float DefaultSamplingInterval = 8f / 60f;
    public const string DebugRcGetPath = "/rysy.interop/settings";
    public const string DebugRcSetPath = "/rysy.interop/settings/set";
    public const string DebugRcSetSamplingIntervalQueryString = "samplingInterval";
    
    public float SamplingInterval { get; set; } = DefaultSamplingInterval;
}
