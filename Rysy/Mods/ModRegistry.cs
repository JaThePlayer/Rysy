﻿using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Scenes;
using System.Reflection;
using System.Text.Json;

namespace Rysy.Mods;

public static class ModRegistry {
    public delegate void ModAssemblyScanner(ModMeta mod, Assembly? oldAssembly);

    internal static ModAssemblyScanner? ModAssemblyScannerInstance;

    private static Dictionary<string, ModMeta> _Mods { get; set; } = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ModMeta> Mods => _Mods.AsReadOnly();

    public static LayeredFilesystem Filesystem { get; private set; } = new();

    /// <summary>
    /// Tries to get a <see cref="ModMeta"/> for a mod using its everest.yaml name.
    /// Returns null if the mod is not loaded.
    /// </summary>
    public static ModMeta? GetModByName(string modName) => _Mods.GetValueOrDefault(modName);

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

        realPath = realPath.Unbackslash();

        foreach (var (_, mod) in Mods) {
            if (realPath.StartsWith(mod.Filesystem.Root, StringComparison.Ordinal)) {
                return mod;
            }
        }

        return null;
    }

    public static async Task LoadAllAsync(string modDir, SimpleLoadTask? task) {
        task?.SetMessage("Registering Mods");

        UnloadAllMods();
        _Mods.Clear();

        Filesystem = new();

        var rysyPluginPath = SettingsHelper.GetFullPath("Plugins", perProfile: false);
        Directory.CreateDirectory(rysyPluginPath);

        using var watch = new ScopedStopwatch("ModRegistry.LoadAll");

        var blacklisted = (Settings.Instance?.ReadBlacklist ?? true) ?
            TryHelper.Try(() =>
                File.ReadAllLines($"{modDir}/blacklist.txt")
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith('#'))
                .Select(l => $"{modDir}/{l}".Unbackslash())
                .ToHashSet()
            ) ?? new(StringComparer.Ordinal) : new(StringComparer.Ordinal);

        var allMods =
            Directory.GetDirectories(modDir).Where(dir => !dir.EndsWith("Cache", StringComparison.Ordinal))
            .Concat(Directory.GetFiles(modDir, "*.zip"))
            .Select(f => f.Unbackslash())
            .Where(f => !blacklisted.Contains(f))
            // add Rysy global plugins
            .Concat(Directory.GetDirectories(rysyPluginPath))
            .Select(f => {
                if (f.EndsWith(".zip", StringComparison.Ordinal))
                    return CreateModAsync(f, zip: true);

                return CreateModAsync(f, zip: false);
            })
            .Append(Task.FromResult(CreateRysyMod()));

        if (CreateVanillaMod() is { } vanilla) {
            allMods = allMods.Append(Task.FromResult(vanilla));
            VanillaMod = vanilla;
        }

        var all = await Task.WhenAll(allMods);

        foreach (var meta in all) {
            Filesystem.AddMod(meta);

            if (_Mods.TryGetValue(meta.Name, out var prevMod)) {
                Logger.Write("ModRegistry", LogLevel.Warning, $"Duplicate mod found: {prevMod.ToString()} [{prevMod.Filesystem.Root}] vs {meta.ToString()} [{meta.Filesystem.Root}]");
            }
            _Mods[meta.Name] = meta;
        }
    }

    public static ModMeta VanillaMod { get; private set; }
    
    private static ModMeta? CreateVanillaMod() => Profile.Instance is null ? null : new() {
        EverestYaml = new() {
            new() {
                Name = "Celeste",
                Version = new(1, 4, 0, 0),
            }
        },
        Filesystem = new FolderModFilesystem($"{Profile.Instance.CelesteDirectory}/Content"),
    };

    private static ModMeta CreateRysyMod() => new() {
        EverestYaml = new() {
            new() {
                Name = "Rysy",
                Version = new(1, 0, 0, 0), // todo: auto-fill
            }
        },
        PluginAssembly = typeof(RysyEngine).Assembly,
        Filesystem =
#if DEBUG
        Directory.Exists("../../../Assets")
        ? new FolderModFilesystem(Path.GetFullPath("../../../Assets"))
        : new FolderModFilesystem("Assets"),

#else
        new FolderModFilesystem($"Assets"),
#endif
    };

    private static void UnloadAllMods() {
        foreach (var (_, mod) in _Mods) {
            mod.Module?.Unload();
            mod.PluginAssembly = null;
        }
    }

    private static async Task<ModMeta> CreateModAsync(string dir, bool zip) {
        var mod = new ModMeta();
        IModFilesystem filesystem = zip ? new ZipModFilesystem(dir.Unbackslash()) : new FolderModFilesystem(dir.Unbackslash());
        await filesystem.InitialScan();

        mod.Filesystem = filesystem;

        try {
            ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));
            LoadModRysyPlugins(mod);
        } catch (Exception e) {
            Logger.Error(e, $"Error loading mod: {dir}");
        }

        try {
            LoadSettings(mod, registerListener: true);
        } catch (Exception e) {
            Logger.Error(e, $"Error loading mod settings for {mod.Name}");
        }


        return mod;
    }

    private static void LoadSettings(ModMeta mod, bool registerListener) {
        if (registerListener)
            mod.OnAssemblyReloaded += (asm) => LoadSettings(mod, registerListener: false);
        // todo: register a file watcher for the json file

        var settingsType = mod.PluginAssembly?.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModSettings))) ?? typeof(ModSettings);

        var path = mod.SettingsFileLocation;
        Directory.CreateDirectory(path.Directory()!);

        if (File.Exists(path)) {
            using var stream = File.OpenRead(path);

            mod.Settings = (ModSettings) JsonSerializer.Deserialize(stream, settingsType, JsonSerializerHelper.DefaultOptions)!;
            mod.Settings.Meta = mod;
        } else {
            mod.Settings = (ModSettings) Activator.CreateInstance(settingsType)!;
            mod.Settings.Meta = mod;

            // we want to save the json with default values, unless we're just using ModSettings as a fallback
            if (mod.Settings.GetType() != typeof(ModSettings))
                mod.Settings.Save();
        }
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
        mod.Module.Meta = mod;
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
            bool reloading = false;

            mod.Filesystem.RegisterFilewatch("Rysy", new() {
                OnChanged = name => {
                    if (!reloading && name.EndsWith(".cs", StringComparison.Ordinal)) {
                        reloading = true;
                        RysyEngine.OnEndOfThisFrame += () => {
                            LoadModRysyPlugins(mod, registerFilewatch: false);
                            reloading = false;
                        };
                    }
                }
            });
        }


        var fileInfo = files
            .Select(f => (mod.Filesystem.TryReadAllText(f)!, $"${mod.Filesystem.Root.FilenameNoExt()}/{f}"))
            .Where(p => p.Item1 is not null).ToList();

        var hasCsproj = mod.Filesystem.FindFilesInDirectoryRecursive("Rysy", "csproj").Any();
        var cachePath = SettingsHelper.GetFullPath($"CompileCache/{mod.Name.ToValidFilename()}", perProfile: true);
        var anyFiles = CodeCompilationHelper.CompileFiles(mod.Name, fileInfo, cachePath, addGlobalUsings: !hasCsproj, out var modAsm, out var emitResult);

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
            mod.EverestYaml = parsedYaml;
            if (mod.EverestYaml.Count != 0) {
                return;
            }
            // there's no actual mod in the yaml?
        }

        var guessedName = guessedNameGetter?.Invoke() ?? $"<unknown:{Guid.NewGuid()}>";
        Logger.Write("ModRegistry", LogLevel.Info, $"Found mod with no everest.yaml or an invalid one: {guessedName} [{filesystem.Root}]");
        mod.EverestYaml = new() {
            new() {
                Name = guessedName,
                Version = new(1, 0, 0, 0),
            }
        };
    }

    private static List<EverestModuleMetadata>? ParseEverestYaml(Stream stream) {
        using var everestYamlReader = new StreamReader(stream);
        var yamls = YamlHelper.Deserializer.Deserialize<List<EverestModuleMetadata>>(everestYamlReader);
        if (yamls.Count != 0) {
            return yamls;
        }

        return null;
    }
}
