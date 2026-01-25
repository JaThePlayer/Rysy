namespace Celeste.Mod.Rysy.InteropMod;

public interface IModLifetimeScoped {
    public void Load();
    
    public void Unload();
}
