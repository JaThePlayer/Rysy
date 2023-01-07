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
    }
}
