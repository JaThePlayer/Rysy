using KeraLua;
using Rysy.LuaSupport;
using YamlDotNet.Serialization;

namespace Rysy.Mods;

public sealed class ModMeta : ILuaWrapper {
    public ModModule Module { get; internal set; }

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
    /// The path of the mod .dll inside the ZIP or the absolute DLL path if in a directory.
    /// </summary>
    public string DLL { get; set; }

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