using Rysy.Mods;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Rysy.HotReloadHandler))]

namespace Rysy;

public static class HotReloadHandler {
    /// <summary>
    /// Called when the application gets Hot Reloaded after a code change.
    /// </summary>
    public static event Action OnHotReload;

    public static void ClearCache(Type[]? types) {
        OnHotReload?.Invoke();
    }
    public static void UpdateApplication(Type[]? types) {
        types?.Select(t => t.Assembly.FullName).LogAsJson();

        if (ModRegistry.GetModByName("Rysy") is { } rysyMod)
            rysyMod.PluginAssembly = typeof(RysyEngine).Assembly;
    }
}
