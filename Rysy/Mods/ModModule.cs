namespace Rysy.Mods;

public class ModModule {
    public ModMeta Meta { get; internal set; }

    internal ComponentRegistryScope ComponentRegistryScope { get; set; }

    public IComponentRegistry ComponentRegistry => ComponentRegistryScope;

    public IRysyLogger Logger => field ??= ComponentRegistry.GetRequired<IRysyLoggerFactory>().CreateLogger(GetType());

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
