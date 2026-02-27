using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Platforms;
using System.Reflection;
using System.Text.Json;

namespace Rysy.Mods;

public static class ModRegistry {
    private const string LogTag = "ModRegistry";
    
    public static ModMeta VanillaMod { get; private set; }
    
    public static ModMeta RysyMod { get; private set; }
    
    public delegate void ModAssemblyScanner(ModMeta mod, Assembly? oldAssembly);

    internal static ModAssemblyScanner? ModAssemblyScannerInstance;

    private static Dictionary<string, ModMeta> ModsMutable { get; set; } = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ModMeta> Mods => ModsMutable.AsReadOnly();

    public static LayeredFilesystem Filesystem { get; private set; } = new();
    
    public static bool IsLoaded { get; private set; }
    
    internal static IComponentRegistry LastUsedComponentRegistry { get; set; }

    /// <summary>
    /// Tries to get a <see cref="ModMeta"/> for a mod using its everest.yaml name.
    /// Returns null if the mod is not loaded.
    /// </summary>
    public static ModMeta? GetModByName(string modName) => ModsMutable.GetValueOrDefault(modName ?? "");

    /// <summary>
    /// Tries to get the settings of mod <paramref name="modName"/>. If the mod doesn't exist, null is returned.
    /// </summary>
    /// <typeparam name="T">The type of the mod's settings class.</typeparam>
    /// <param name="modName">The name of the mod whose settings you want to get.</param>
    /// <returns>The mod's settings, or null if the mod doesn't exist</returns>
    public static T? GetModSettings<T>(string modName) where T : ModSettings {
        if (GetModByName(modName) is not { } mod)
            return null;

        return (T?) mod.Settings;
    }

    public static void RegisterModAssemblyScanner(ModAssemblyScanner scanner) {
        // make sure that the scanner is caught up with the currently loaded mods
        foreach (var mod in Mods.Values) {
            if (mod.PluginAssembly is { } asm) {
                scanner(mod, mod.PluginAssembly);
            }
        }

        ModAssemblyScannerInstance += scanner;
    }

    public static void DeregisterModAssemblyScanner(ModAssemblyScanner scanner) {
        ModAssemblyScannerInstance -= scanner;
    }

    public static ModMeta? GetModContainingRealPath(string? realPath) {
        if (realPath is null)
            return null;

        var unbackslashed = Interpolator.Shared.Clone(realPath);
        unbackslashed.Replace('\\', '/');

        foreach (var (_, mod) in Mods) {
            if (unbackslashed.StartsWith(mod.Filesystem.Root)) {
                // the root is correct, check if the file actually exists there though
                var vpath = unbackslashed[mod.Filesystem.Root.Length..].TrimStart('/').ToString();
                if (mod.Filesystem.FileExists(vpath))
                    return mod;
            }
        }

        return null;
    }
    
    public static void NotifyFileCreatedAtRealPath(string? realPath) {
        if (realPath is null)
            return;
        
        var unbackslashed = Interpolator.Shared.Clone(realPath);
        unbackslashed.Replace('\\', '/');

        foreach (var (_, mod) in Mods) {
            if (unbackslashed.StartsWith(mod.Filesystem.Root)) {
                var vpath = unbackslashed[mod.Filesystem.Root.Length..].TrimStart('/').ToString();
                mod.Filesystem.NotifyFileCreated(vpath);
            }
        }
    }

    public static async Task LoadAllAsync(string modDir, IComponentRegistry componentRegistry, SimpleLoadTask? task, bool loadCSharpPlugins = true) {
        LastUsedComponentRegistry = componentRegistry;
        task?.SetMessage("Registering Mods");

        UnloadAllMods();
        ModsMutable.Clear();

        Filesystem = new();

        using var watch = new ScopedStopwatch("ModRegistry.LoadAll");

        var blacklisted = (Settings.Instance?.ReadBlacklist ?? true) ?
            TryHelper.Try(() =>
                File.ReadAllLines($"{modDir}/blacklist.txt")
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith('#'))
                .Select(l => $"{modDir}/{l}".Unbackslash())
                .ToHashSet()
            ) ?? new(StringComparer.Ordinal) : new(StringComparer.Ordinal);

        IEnumerable<Task<ModMeta?>> allMods =
            Directory.GetDirectories(modDir).Where(dir => !dir.EndsWith("Cache", StringComparison.Ordinal))
            .Concat(Directory.GetFiles(modDir, "*.zip"))
            .Select(f => f.Unbackslash())
            .Where(f => !blacklisted.Contains(f))
            .Select(f => {
                if (f.EndsWith(".zip", StringComparison.Ordinal))
                    return CreateModAsync(f, componentRegistry, zip: true, loadCSharpPlugins);

                return CreateModAsync(f, componentRegistry, zip: false, loadCSharpPlugins);
            })!
            .Append(Task.FromResult(CreateRysyMod(componentRegistry)))!;

        if (CreateVanillaMod() is { } vanilla) {
            allMods = allMods!.Append(Task.FromResult(vanilla))!;
            VanillaMod = vanilla;
        }

        var all = await Task.WhenAll(allMods);

        foreach (var meta in all) {
            if (meta is not null)
                RegisterMod(componentRegistry, meta);
        }

        IsLoaded = true;
    }

    private static void RegisterMod(IComponentRegistry componentRegistry, ModMeta meta)
    {
        Filesystem.AddMod(meta);

        if (ModsMutable.TryGetValue(meta.Name, out var prevMod)) {
            Logger.Write(LogTag, LogLevel.Warning, $"Duplicate mod found: {prevMod.ToString()} [{prevMod.Filesystem.Root}] vs {meta.ToString()} [{meta.Filesystem.Root}]");
        }
        ModsMutable[meta.Name] = meta;
    }

    public static ModMeta CreateNewMod(string id) {
        var dir = Path.Combine(Profile.Instance.ModsDirectory, id.ToValidFilename());

        Directory.CreateDirectory(dir);

        var meta = new ModMeta {
            Filesystem = new FolderModFilesystem(dir.Unbackslash()),
            EverestYaml = [
                new EverestModuleMetadata {
                    Name = id,
                    Version = new(1,0,0),
                    Dependencies = [],
                }
            ],
        };

        meta.TrySaveEverestYaml();

        RegisterMod(LastUsedComponentRegistry, meta);

        return meta;
    }
    
    private static ModMeta? CreateVanillaMod() => Profile.Instance is null ? null : new() {
        EverestYaml = new() {
            new() {
                Name = "Celeste",
                Version = new(1, 4, 0, 0),
            }
        },
        Filesystem = new ReadonlyModFilesystem(new FolderModFilesystem($"{Profile.Instance.CelesteDirectory}/Content")),
    };

    private static ModMeta CreateRysyMod(IComponentRegistry registry) {
        RysyMod = new() {
            EverestYaml = [
                new() {
                    Name = "Rysy", Version = RysyEngine.Version
                }
            ],
            Filesystem = RysyPlatform.Current.GetRysyFilesystem(),
        };

        LoadModule(RysyMod, typeof(RysyEngine).Assembly, registry);
        
        return RysyMod;
    }

    private static void UnloadAllMods() {
        IsLoaded = false;

        foreach (var (_, mod) in ModsMutable) {
            mod.Module?.Unload();
            mod.Module?.ComponentRegistryScope.Dispose();
            mod.Module = null;
            mod.PluginAssembly = null;
        }
    }

    private static async Task<ModMeta?> CreateModAsync(string dir, IComponentRegistry componentRegistry, bool zip, bool loadCSharp) {
        IModFilesystem? filesystem;
        try {
            filesystem = zip ? new ZipModFilesystem(dir.Unbackslash()) : new FolderModFilesystem(dir.Unbackslash());
            await filesystem.InitialScan();
        } catch (Exception e) {
            Logger.Error(LogTag, e, $"Failed to create filesystem for mod at {dir.Unbackslash().Censor()}. Skipping loading the mod!");
            return null;
        }

        var mod = new ModMeta {
            Filesystem = filesystem,
        };

        mod.EverestYaml = ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));
        
        try {
            if (loadCSharp)
                LoadModRysySourceCodePlugins(mod, componentRegistry);
        } catch (Exception e) {
            Logger.Error(LogTag, e, $"Error loading mod: {dir}");
        }

        try {
            LoadSettings(mod);
        } catch (Exception e) {
            Logger.Error(LogTag, e, $"Error loading mod settings for {mod.Name}");
        }

        return mod;
    }

    internal static void LoadSettings(ModMeta mod) {
        // todo: register a file watcher for the json file

        var settingsType = mod.PluginAssembly?.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModSettings))) ?? typeof(ModSettings);

        var path = mod.SettingsFileLocation;
        var fs = SettingsHelper.GetFilesystem(perProfile: false);

        if (fs.TryReadAllText(path) is { } settingsJson) {
            mod.Settings = (ModSettings) JsonSerializer.Deserialize(settingsJson, settingsType, JsonSerializerHelper.DefaultOptions)!;
            mod.Settings.Meta = mod;
        } else {
            mod.Settings = (ModSettings) Activator.CreateInstance(settingsType)!;
            mod.Settings.Meta = mod;

            // we want to save the json with default values, unless we're just using ModSettings as a fallback
            if (mod.Settings.GetType() != typeof(ModSettings))
                mod.Settings.Save();
        }
    }

    internal static void LoadModule(ModMeta mod, Assembly asm, IComponentRegistry componentRegistry) {
        if (mod.Module is { } oldMod) {
            // if we're hot-reloading, .Module will point to the old module type, let's unload it.
            oldMod.Unload();
            oldMod.ComponentRegistryScope.Dispose();
        }

        if (asm.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModModule))) is not { } moduleType) {
            mod.Module = null;
            return;
        }

        try {
            mod.Module = (ModModule) Activator.CreateInstance(moduleType)!;
        } catch (Exception ex) {
            mod.Module = null;
            Logger.Error(LogTag, ex, $"Failed to instantiate mod module {moduleType} for mod {mod.Name}.");
            return;
        }

        CreateScopeAndRegisterModuleForMod(mod, componentRegistry);

        mod.Module.Meta = mod;
        mod.PluginAssembly = asm;

        mod.Module.Load();
    }

    private static void CreateScopeAndRegisterModuleForMod(ModMeta mod, IComponentRegistry componentRegistry)
    {
        if (mod.Module is null)
            throw new ArgumentException($"The mod {mod.Name} doesn't have a module to attach a scope to.", nameof(mod));
        var scope = new ComponentRegistryScope(componentRegistry);
        mod.Module.ComponentRegistryScope = scope;
        scope.Add(mod.Module);
    }

    private static void LoadModRysySourceCodePlugins(ModMeta mod, IComponentRegistry componentRegistry, bool registerFilewatch = true) {
        var fs = mod.Filesystem;
        // Only find dlls in Rysy/bin, not in subfolders - makes sure we don't load both the Debug and Release variants at once.
        var dlls = mod.Filesystem.FindFilesInDirectory("Rysy/bin", "dll")
            .ToArray();

        var ctx = mod.AssemblyLoadContext;

        foreach (var dll in dlls) {
            Logger.Write(LogTag, LogLevel.Info, $"Loading mod assembly for {mod.Name}: {dll}");
            fs.TryWatchAndOpen(dll, stream => {
                // TODO: use asmresolver to find modmodule class ahead of time, and only load those dlls (and error on multiple).
                var modAsm = ctx.LoadFromStream(stream);

                LoadModule(mod, modAsm, componentRegistry);
            }, out var watcher);
            
            if (watcher is {})
                componentRegistry.Add(watcher);
        }
    }

    private static List<EverestModuleMetadata> ReadEverestYaml(ModMeta mod, Func<string?>? guessedNameGetter) {
        var filesystem = mod.Filesystem ?? throw new Exception($"{nameof(mod)}.{nameof(ModMeta.Filesystem)} needs to be set before calling {nameof(ReadEverestYaml)}!");
        var guessedName = guessedNameGetter?.Invoke() ?? $"<unknown:{Guid.NewGuid()}>";

        List<EverestModuleMetadata>? parsedYaml;
        try {
            parsedYaml = filesystem.OpenFile("everest.yaml", ParseEverestYaml)
                         ?? filesystem.OpenFile("everest.yml", ParseEverestYaml);
        } catch (Exception ex) {
            Logger.Error(LogTag, ex, $"Failed to parse everest.yaml for: {guessedName} [{filesystem.Root}]");
            goto returnPlaceholder;
        }

        if (parsedYaml is null or []) {
            Logger.Write(LogTag, LogLevel.Info, $"Found mod with no everest.yaml or an invalid one: {guessedName} [{filesystem.Root}]");
            goto returnPlaceholder;
        }

        if (parsedYaml.All(m => m.IsValid())) {
            return parsedYaml;
        }
        
        returnPlaceholder:
        return [
            new() {
                Name = guessedName,
                Version = new(1, 0, 0, 0),
            }
        ];
    }

    private static List<EverestModuleMetadata>? ParseEverestYaml(Stream stream) {
        using var everestYamlReader = new StreamReader(stream);
        var yamls = YamlHelper.Deserializer.Deserialize<List<EverestModuleMetadata>>(everestYamlReader);
        if (yamls is [_, ..]) {
            return yamls;
        }

        return null;
    }

    public static bool IsVanillaModName(string s) {
        return s is "Celeste" or "Rysy" or "Everest";
    }
}
