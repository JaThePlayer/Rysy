namespace Rysy.Mods;

public class ModModule {
    public ModMeta Meta { get; internal set; }

    public virtual void Load() {

    }

    public virtual void Unload() {

    }

    public T? GetSettings<T>() where T : ModSettings {
        return (T?)Meta.Settings;
    }

    public void SaveSettings() {
        Meta.Settings?.Save();
    }
}
