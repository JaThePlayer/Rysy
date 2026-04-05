using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Signals;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Rysy.Mods;

public sealed class ModMeta : ISignalEmitter {
    internal ModMeta() { }

    /// <summary>
    /// The module class of this mod.
    /// </summary>
    public ModModule? Module { get; internal set; }

    /// <summary>
    /// The assembly containing this plugin's code.
    /// </summary>
    public Assembly? PluginAssembly {
        get;
        internal set {
            var oldAsm = field;
            field = value;

            ModRegistry.LoadSettings(this);
            ModRegistry.ModAssemblyScannerInstance?.Invoke(this, oldAsm);
            this.Emit(new ModAssemblyReloaded(this, oldAsm, value));
        }
    }

    public ModSettings? Settings {
        get;
        internal set {
            if (field is {})
                Module?.ComponentRegistryScope.Remove(field);

            field = value;

            if (value is {})
                Module?.ComponentRegistryScope.Add(value);
            // todo: lonn bindings
        }
    }

    /// <summary>
    /// The filesystem for this mod, used for retrieving assets contained in the mod.
    /// </summary>
    public required IModFilesystem Filesystem { get; init; }

    public AssemblyLoadContext AssemblyLoadContext => field ??= new(Name, isCollectible: true);

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
    public List<EverestModuleMetadata> EverestYaml { get; internal set; } = [];

    /// <summary>
    /// The mod name, taken from the everest.yaml
    /// </summary>
    public string Name => EverestYaml.FirstOrDefault()?.Name ?? $"<Unknown:{Path.GetFileName(Filesystem.Root)}>";

    /// <summary>
    /// Display name of the mod, taking into consideration the lang file.
    /// </summary>
    public string DisplayName => ModNameToDisplayName(Name);

    /// <summary>
    /// The mod version, taken from the everest.yaml
    /// </summary>
    public Version Version => EverestYaml.FirstOrDefault()?.Version ?? new Version();

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

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
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
    [YamlMember(Alias = "DLL", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
    public string? Dll { get; set; }

    /// <summary>
    /// The mod version.
    /// </summary>
    [YamlIgnore]
    public Version Version { get; set; } = new Version(1, 0);

    [YamlMember(Alias = "Version")]
    public string VersionString {
        get => field ?? Version.ToString();
        set {
            ArgumentNullException.ThrowIfNull(value);

            field = value;
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
    public List<EverestDependency> Dependencies { get; set; } = [];

    /// <summary>
    /// The optional dependencies of the mod. This mod will load after the mods listed here if they are installed; if they aren't, the mod will load anyway.
    /// </summary>
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
    public List<EverestDependency> OptionalDependencies { get; set; } = [];

    public override string ToString() {
        return Name + " " + Version;
    }

    public bool IsValid() {
        return !Name.IsNullOrWhitespace();
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

    [YamlMember(Alias = "Version")]
    public string VersionString {
        get => field ?? Version.ToString();
        set {
            ArgumentNullException.ThrowIfNull(value);

            field = value;
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