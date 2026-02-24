using Rysy.Components;
using Rysy.Signals;

namespace Rysy.Mods;

internal sealed class RysyModModule : ModModule, ISignalListener<ViewportChanged> {
    public void OnSignal(ViewportChanged signal) {
        Logger.Info($"ViewportChanged: {signal}");
    }
}