using Rysy.Helpers;
using Rysy.Layers;

namespace Rysy.Mods;

internal sealed class RysyModModule : ModModule {
    public override void Load() {
        base.Load();

        ComponentRegistry.AddIfMissing<PrefabHelper>();
        EditorLayers.RegisterVanillaLayers(ComponentRegistry);
    }
}