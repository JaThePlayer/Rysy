using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;
using System.Reflection;

namespace Rysy.Mods;

public static class ModRegistry {
    private static Dictionary<string, ModMeta> _Mods { get; set; } = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ModMeta> Mods => _Mods.AsReadOnly();

    public static LayeredFilesystem Filesystem { get; private set; } = new();

    /// <summary>
    /// Tries to get a <see cref="ModMeta"/> for a mod using its everest.yaml name.
    /// Returns null if the mod is not loaded.
    /// </summary>
    public static ModMeta? GetModByName(string modName) => _Mods.GetValueOrDefault(modName);

    public static async Task LoadAllAsync(string modDir) {
        LoadingScene.Text = "Scanning For Mods";

        UnloadAllMods();
        _Mods.Clear();

        Filesystem = new();

        var rysyPluginPath = SettingsHelper.GetFullPath("Plugins", perProfile: false);
        Directory.CreateDirectory(rysyPluginPath);

        using var watch = new ScopedStopwatch("ModRegistry.LoadAll");

        var blacklisted = TryHelper.Try(() => 
            File.ReadAllLines($"{modDir}/blacklist.txt")
            .Select(l => l.Trim())
            .Where(l => !l.StartsWith('#'))
            .Select(l => $"{modDir}/{l}".Unbackslash())
            .ToHashSet()
        ) ?? new();

        var allMods =
            Directory.GetDirectories(modDir).Where(dir => !dir.EndsWith("Cache"))
            .Concat(Directory.GetFiles(modDir, "*.zip"))
            .Select(f => f.Unbackslash())
            .Where(f => !blacklisted.Contains(f))
            // add Rysy global plugins
            .Concat(Directory.GetDirectories(rysyPluginPath))
            .Select(async f => {
                if (f.EndsWith(".zip"))
                    return await CreateModAsync(f, zip: true);

                return await CreateModAsync(f, zip: false);
            })
            .Append(Task.FromResult(CreateRysyMod()));

        if (CreateVanillaMod() is { } vanilla) {
            allMods = allMods.Append(Task.FromResult(vanilla));
        }

        var all = await Task.WhenAll(allMods);

        foreach (var meta in all) {
            Filesystem.AddMod(meta);

            if (_Mods.TryGetValue(meta.Name, out var prevMod)) {
                Logger.Write("ModRegistry", LogLevel.Warning, $"Duplicate mod found: {prevMod.EverestYaml} [{prevMod.Filesystem.Root}] vs {meta.EverestYaml} [{meta.Filesystem.Root}]");
            }
            _Mods[meta.Name] = meta;
        }
    }

    private static ModMeta? CreateVanillaMod() => Profile.Instance is null ? null : new() {
        EverestYaml = new() {
            Name = "Celeste",
            Version = new(1, 4, 0, 0),
        },
        Filesystem = new FolderModFilesystem($"{Profile.Instance.CelesteDirectory}/Content"),
    };

    private static ModMeta CreateRysyMod() => new() {
        EverestYaml = new() {
            Name = "Rysy",
            Version = new(1, 0, 0, 0), // todo: auto-fill
        },
        Filesystem = new FolderModFilesystem($"Assets"),
    };

    private static void UnloadAllMods() {
        foreach (var (_, mod) in _Mods) {
            mod.Module?.Unload();
        }
    }

    private static async Task<ModMeta> CreateModAsync(string dir, bool zip) {
        var mod = new ModMeta();
        IModFilesystem filesystem = zip ? new ZipModFilesystem(dir) : new FolderModFilesystem(dir);
        await filesystem.InitialScan();

        mod.Filesystem = filesystem;

        try {
            ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));
            LoadModRysyPlugins(mod);
        } catch (Exception e) {
            Logger.Error(e, $"Error loading mod: {dir}");
        }


        return mod;
    }

    private static void LoadModule(ModMeta mod) {
        if (mod.Module is { } oldMod) {
            // if we're hot-reloading, .Module will point to the old module type, let's unload it.
            oldMod.Unload();
        }

        if (mod.PluginAssembly is not { } asm) {
            mod.Module = new();
            return;
        }

        if (asm.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModModule))) is not { } moduleType) {
            mod.Module = new();
            return;
        }

        mod.Module = (ModModule) Activator.CreateInstance(moduleType)!;
        mod.Module.Load();
    }

    private static void LoadModRysyPlugins(ModMeta mod, bool registerFilewatch = true) {
        var files = mod.Filesystem.FindFilesInDirectoryRecursive("Rysy", "cs")
            .Where(f => !f.StartsWith("Rysy/obj", StringComparison.Ordinal)
                     && !f.StartsWith("Rysy/.vs", StringComparison.Ordinal)
                     && !f.StartsWith("Rysy/bin", StringComparison.Ordinal))
            .ToArray();
        if (files.Length == 0)
            return;

        if (registerFilewatch) {
            /*
             foreach (var file in files) {
                mod.Filesystem.RegisterFilewatch(file, new() {
                    OnChanged = stream => RysyEngine.OnEndOfThisFrame += () => LoadModRysyPlugins(mod, registerFilewatch: false)
                });
            }
             */
            mod.Filesystem.RegisterFilewatch("Rysy", new() {
                OnChanged = stream => RysyEngine.OnEndOfThisFrame += () => LoadModRysyPlugins(mod, registerFilewatch: false)
            });
        }


        var fileInfo = files.Select(f => (mod.Filesystem.TryReadAllText(f)!, $"${mod.Filesystem.Root.FilenameNoExt()}/{f}")).Where(p => p.Item1 is not null).ToList();

        var cachePath = SettingsHelper.GetFullPath($"CompileCache/{mod.Name.ToValidFilename()}", perProfile: true);
        var anyFiles = CodeCompilationHelper.CompileFiles(mod.Name, fileInfo, cachePath, out var modAsm, out var emitResult);

        if (!anyFiles)
            return;

        if (emitResult is { } && !emitResult.Success) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Warning, $"Failed compiling Rysy .cs plugins for: {mod.Name}:\n{emitResult.Diagnostics.FormatDiagnostics()}");
            return;
        }

        if (emitResult is { Success: true }) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Info, $"Successfully compiled Rysy .cs plugins for: {mod.Name}");
        } else if (modAsm is { }) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Info, $"Successfully loaded cached Rysy .cs plugins for: {mod.Name}");
        }

        mod.PluginAssembly = modAsm;

        LoadModule(mod);
    }

    private static void ReadEverestYaml(ModMeta mod, Func<string?>? guessedNameGetter) {
        var filesystem = mod.Filesystem ?? throw new Exception($"{nameof(mod)}.{nameof(ModMeta.Filesystem)} needs to be set before calling {nameof(ReadEverestYaml)}!");

        var parsedYaml = filesystem.OpenFile("everest.yaml", ParseEverestYaml)
                      ?? filesystem.OpenFile("everest.yml", ParseEverestYaml);

        if (parsedYaml is { }) {
            // todo: handle multiple mods in one yaml
            mod.EverestYaml = parsedYaml[0];
            return;
        }

        var guessedName = guessedNameGetter?.Invoke() ?? $"<unknown:{Guid.NewGuid()}>";
        Logger.Write("ModRegistry", LogLevel.Info, $"Found mod with no everest.yaml: {guessedName} [{filesystem.Root}]");
        mod.EverestYaml = new() {
            Name = guessedName,
            Version = new(1, 0, 0, 0),
        };
    }

    private static EverestModuleMetadata[]? ParseEverestYaml(Stream stream) {
        using var everestYamlReader = new StreamReader(stream);
        var yamls = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata[]>(everestYamlReader);
        if (yamls.Length != 0) {
            return yamls;
        }

        return null;
    }
}
