using Rysy.InteropMod.InRysy.EventRenderers;
using Rysy.Mods;

namespace Rysy.InteropMod.InRysy;

public sealed class InteropModModule : ModModule
{
    public static InteropModModule Instance { get; private set; }
    
    public override void Load() {
        Instance = this;
        
        base.Load();

        var eventRendererManager = new EventRendererManager()
            .RegisterRenderer(new PlayerTrailEventRenderer());
        ComponentRegistry.Add(eventRendererManager);
        ComponentRegistry.Add(new RecordingDataRetriever(eventRendererManager));
    }

    public override void Unload() {
        base.Unload();
    }
}
