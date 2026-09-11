using Rysy.Shared.InteropMod;

namespace Celeste.Mod.Rysy.InteropMod;

public class InteropModModuleSettings : EverestModuleSettings {
    public InGameSettings RemoteSettings { get; set; } = new InGameSettings();
}
