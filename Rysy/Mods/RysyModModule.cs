using Rysy.Components;
using Rysy.Graphics;
using Rysy.Gui.WindowManagers;
using Rysy.Helpers;
using Rysy.Layers;

namespace Rysy.Mods;

internal sealed class RysyModModule : ModModule {
    public override void Load() {
        base.Load();

        ComponentRegistry.AddIfMissing<PrefabHelper>();
        EditorLayers.RegisterVanillaLayers(ComponentRegistry);

        ComponentRegistry.AddIfMissing<TilingWindowManager>();
        ComponentRegistry.AddIfMissing<OffsetFromExistingManager>();
        
        ComponentRegistry.Add(new EntityListSpriteProvider(EditorLayers.Entities, p => p.EntitiesVisible));
        ComponentRegistry.Add(new EntityListSpriteProvider(EditorLayers.Triggers, p => p.TriggersVisible));
        ComponentRegistry.Add(new EntityListSpriteProvider(EditorLayers.BgDecals, p => p.BgDecalsVisible));
        ComponentRegistry.Add(new EntityListSpriteProvider(EditorLayers.FgDecals, p => p.FgDecalsVisible));
        ComponentRegistry.Add(new TileGridSpriteProvider(EditorLayers.Bg, p => p.BgTilesVisible));
        ComponentRegistry.Add(new TileGridSpriteProvider(EditorLayers.Fg, p => p.FgTilesVisible));
        
        TilesetTemplates.RegisterDefaultTemplates(ModRegistry.Filesystem, ComponentRegistry);
    }
}