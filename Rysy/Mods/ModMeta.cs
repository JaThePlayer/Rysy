using KeraLua;
using Rysy.Helpers;
using Rysy.LuaSupport;
using System.Reflection;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Rysy.Mods;

public sealed class ModMeta {
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

    public LayeredFilesystem GetAllDependenciesFilesystem(bool includeOptionalDeps = true) {
        var fs = new LayeredFilesystem();
        var addedMods = new HashSet<string>() { Name };
        var mods = new List<ModMeta>();

        fs.AddMod(this);
        fs.AddMod(ModRegistry.VanillaMod);

        foreach (var yaml in EverestYaml) {
            AppendDependencies(mods, addedMods, yaml, includeOptionalDeps);
            foreach (var mod in mods) {
                fs.AddMod(mod);
            }
        }


        return fs;
    }

    public IEnumerable<ModMeta> GetAllDependenciesRecursive(bool includeOptionalDeps = false) {
        var addedMods = new HashSet<string>() { Name };
        var mods = new List<ModMeta>();

        foreach (var yaml in EverestYaml)
            AppendDependencies(mods, addedMods, yaml, includeOptionalDeps);

        return mods;
    }

    private static void AppendDependencies(List<ModMeta> mods, HashSet<string> addedMods, EverestModuleMetadata meta, bool includeOptionalDeps = false) {
        var deps = includeOptionalDeps ? meta.Dependencies.Concat(meta.OptionalDependencies) : meta.Dependencies;

        foreach (var dep in deps) {
            if (addedMods.Contains(dep.Name))
                continue;

            var mod = ModRegistry.GetModByName(dep.Name);
            if (mod is null)
                continue;

            mods.Add(mod);
            addedMods.Add(dep.Name);

            AppendDependencies(mods, addedMods, mod.EverestYaml.Find(meta => meta.Name == dep.Name)!);
        }
    }

    /// <summary>
    /// The metadata stored in the everest.yaml for this mod
    /// </summary>
    public List<EverestModuleMetadata> EverestYaml { get; internal set; }

    /// <summary>
    /// The mod name, taken from the everest.yaml
    /// </summary>
    public string Name => EverestYaml.First().Name;

    /// <summary>
    /// Display name of the mod, taking into consideration the lang file.
    /// </summary>
    public string DisplayName => ModNameToDisplayName(Name);

    /// <summary>
    /// The mod version, taken from the everest.yaml
    /// </summary>
    public Version Version => EverestYaml.First().Version;

    public bool IsVanilla => Name is "Rysy" or "Celeste";

    public bool IsRysy => Name is "Rysy";
    
    [JsonIgnore]
    public string SettingsFileLocation => $"ModSettings/{Name.ToValidFilename()}.json";

    public override string ToString() => string.Join(',', EverestYaml.Select(x => x.ToString()));

    public bool DependencyMet(ModMeta other) {
        if (Name == other.Name && Version <= other.Version) {
            return true;
        }
        if (ModRegistry.IsVanillaModName(other.Name))
            return true;
        
        foreach (var meta in EverestYaml) {
            foreach (var dep in meta.Dependencies) {
                if (dep.Name == other.Name && dep.Version <= other.Version) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool DependencyMet(string otherName) {
        if (Name == otherName)
            return true;
        if (ModRegistry.IsVanillaModName(otherName))
            return true;
        
        foreach (var meta in EverestYaml) {
            foreach (var dep in meta.Dependencies) {
                if (dep.Name == otherName) {
                    return true;
                }
            }
        }

        return false;
    }

    public static string ModNameToDisplayName(string modname) {
        return Interpolator.Temp($"mods.{modname}.name").TranslateOrNull() ?? modname;
    }

    public bool TrySaveEverestYaml() {
        if (Filesystem is not IWriteableModFilesystem fs)
            return false;
        
        var yaml = YamlHelper.Serializer.Serialize(EverestYaml);
        var yamlPath = fs.FileExists("everest.yml") ? "everest.yml" : "everest.yaml";
        
        if (fs.FileExists(yamlPath))
            fs.CopyFileTo(yamlPath, yamlPath + ".backup");
        
        return fs.TryWriteToFile(yamlPath, yaml);
    }
}

/// <summary>
/// Any module metadata, usually mirroring the data in your metadata.yaml.
/// Copied from https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Module/EverestModuleMetadata.cs
/// </summary>
public sealed class EverestModuleMetadata : ILuaWrapper {
    /// <summary>
    /// The name of the mod.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The path to the dll of the mod.
    /// Unused by Rysy, but still read to not break the yaml upon saving.
    /// </summary>
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
    public string? DLL { get; set; }

    /// <summary>
    /// The mod version.
    /// </summary>
    [YamlIgnore]
    public Version Version { get; set; } = new Version(1, 0);
    private string _VersionString;
    [YamlMember(Alias = "Version")]
    public string VersionString {
        get => _VersionString ?? Version.ToString();
        set {
            ArgumentNullException.ThrowIfNull(value);

            _VersionString = value;
            int versionSplitIndex = value.IndexOf('-', StringComparison.Ordinal);
            if (versionSplitIndex == -1)
                Version = new Version(value);
            else
                Version = new Version(value[..versionSplitIndex]);
        }
    }

    /// <summary>
    /// The dependencies of the mod.
    /// </summary>
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
    public List<EverestDependency> Dependencies { get; set; } = new List<EverestDependency>();

    /// <summary>
    /// The optional dependencies of the mod. This mod will load after the mods listed here if they are installed; if they aren't, the mod will load anyway.
    /// </summary>
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
    public List<EverestDependency> OptionalDependencies { get; set; } = new List<EverestDependency>();

    public override string ToString() {
        return Name + " " + Version;
    }

    public bool IsValid() {
        return !Name.IsNullOrWhitespace() && Version != null;
    }
    
    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
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

public class EverestDependency {
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
        get => _VersionString ?? Version.ToString();
        set {
            ArgumentNullException.ThrowIfNull(value);

            _VersionString = value;
            int versionSplitIndex = value.IndexOf('-', StringComparison.Ordinal);
            if (versionSplitIndex == -1)
                Version = new Version(value);
            else
                Version = new Version(value[..versionSplitIndex]);
        }
    }

    public override string ToString() {
        return Name + " " + Version;
    }

    public EverestModuleMetadata ToModuleMetadata() => new() {
        Name = Name,
        Version = Version,
    };
}