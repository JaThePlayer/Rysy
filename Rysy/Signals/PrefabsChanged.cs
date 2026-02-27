using Rysy.Helpers;

namespace Rysy.Signals;

public record struct PrefabsChanged(PrefabHelper Helper) : ISignal;