using KeraLua;
using Rysy.Extensions;
using Rysy.LuaSupport;
using System.Reflection;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Rysy.Mods;

public sealed class ModMeta : ILuaWrapper {
    internal ModMeta() { }

    /// <summary>
    /// The module class of this mod.
    /// </summary>
    public ModModule? Module { get; internal set; }

    private Assembly? _PluginAssembly;

    /// <summary>
    /// The assembly containing this plugin's code.
    /// </summary>
    public Assembly? PluginAssembly {
        get => _PluginAssembly;
        internal set {
            var oldAsm = _PluginAssembly;
            _PluginAssembly = value;

            ModRegistry.ModAssemblyScannerInstance?.Invoke(this, oldAsm);
            OnAssemblyReloaded?.Invoke(value);
        } 
    }

    private ModSettings? _Settings;
    public ModSettings? Settings {
        get => _Settings;
        internal set {
            _Settings = value;
            // todo: lonn bindings
        }
    }

    /// <summary>
    /// Gets called whenever the <see cref="PluginAssembly"/> gets reloaded.
    /// </summary>
    public event Action<Assembly?> OnAssemblyReloaded;

    /// <summary>
    /// The filesystem for this mod, used for retrieving assets contained in the mod.
    /// </summary>
    public IModFilesystem Filesystem { get; internal set; }

    /// <summary>
    /// The metadata stored in the everest.yaml for this mod
    /// </summary>
    public EverestModuleMetadata EverestYaml { get; internal set; }

    /// <summary>
    /// The mod name, taken from the everest.yaml
    /// </summary>
    public string Name => EverestYaml.Name;

    /// <summary>
    /// The mod version, taken from the everest.yaml
    /// </summary>
    public Version Version => EverestYaml.Version;

    [JsonIgnore]
    public string SettingsFileLocation => SettingsHelper.GetFullPath($"ModSettings/{Name.ToValidFilename()}.json", perProfile: false);

    public int Lua__index(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "Name":
                lua.PushString(Name);
                return 1;
            case "Version":
                lua.PushString(Version.ToString());
                return 1;
        }

        lua.PushNil();
        return 1;
    }

    public override string ToString() => EverestYaml.ToString();
}

/// <summary>
/// Any module metadata, usually mirroring the data in your metadata.yaml.
/// Copied from https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Module/EverestModuleMetadata.cs
/// </summary>
public sealed class EverestModuleMetadata {
    /// <summary>
    /// The name of the mod.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The mod version.
    /// </summary>
    [YamlIgnore]
    public Version Version { get; set; } = new Version(1, 0);
    private string _VersionString;
    [YamlMember(Alias = "Version")]
    public string VersionString {
        get => _VersionString;
        set {
            _VersionString = value;
            int versionSplitIndex = value.IndexOf('-');
            if (versionSplitIndex == -1)
                Version = new Version(value);
            else
                Version = new Version(value[..versionSplitIndex]);
        }
    }

    /// <summary>
    /// The dependencies of the mod.
    /// </summary>
    public List<EverestModuleMetadata> Dependencies { get; set; } = new List<EverestModuleMetadata>();

    /// <summary>
    /// The optional dependencies of the mod. This mod will load after the mods listed here if they are installed; if they aren't, the mod will load anyway.
    /// </summary>
    public List<EverestModuleMetadata> OptionalDependencies { get; set; } = new List<EverestModuleMetadata>();

    public override string ToString() {
        return Name + " " + Version;
    }
}