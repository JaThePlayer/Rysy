using Rysy.Mods;

namespace Rysy.InteropMod.InRysy;

public sealed class InteropModModule : ModModule
{
    public static InteropModModule Instance { get; private set; }
    
    public override void Load() {
        Instance = this;
        
        base.Load();
        
        ComponentRegistry.Add(new PlayerTrailRenderer());
    }

    public override void Unload() {
        base.Unload();
    }
}
