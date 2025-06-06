﻿using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Platforms;
using System.Reflection;
using System.Text.Json;

namespace Rysy.Mods;

public static class ModRegistry {
    public static ModMeta VanillaMod { get; private set; }
    
    public static ModMeta RysyMod { get; private set; }
    
    public delegate void ModAssemblyScanner(ModMeta mod, Assembly? oldAssembly);

    internal static ModAssemblyScanner? ModAssemblyScannerInstance;

    private static Dictionary<string, ModMeta> _Mods { get; set; } = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ModMeta> Mods => _Mods.AsReadOnly();

    public static LayeredFilesystem Filesystem { get; private set; } = new();

    /// <summary>
    /// Tries to get a <see cref="ModMeta"/> for a mod using its everest.yaml name.
    /// Returns null if the mod is not loaded.
    /// </summary>
    public static ModMeta? GetModByName(string modName) => _Mods.GetValueOrDefault(modName ?? "");

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

        foreach (var (_, mod) in _Mods) {
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

        foreach (var (_, mod) in _Mods) {
            if (unbackslashed.StartsWith(mod.Filesystem.Root)) {
                var vpath = unbackslashed[mod.Filesystem.Root.Length..].TrimStart('/').ToString();
                mod.Filesystem.NotifyFileCreated(vpath);
            }
        }
    }

    public static async Task LoadAllAsync(string modDir, SimpleLoadTask? task, bool loadCSharpPlugins = true) {
        task?.SetMessage("Registering Mods");

        UnloadAllMods();
        _Mods.Clear();

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

        var allMods =
            Directory.GetDirectories(modDir).Where(dir => !dir.EndsWith("Cache", StringComparison.Ordinal))
            .Concat(Directory.GetFiles(modDir, "*.zip"))
            .Select(f => f.Unbackslash())
            .Where(f => !blacklisted.Contains(f))
            .Select(f => {
                if (f.EndsWith(".zip", StringComparison.Ordinal))
                    return CreateModAsync(f, zip: true, loadCSharpPlugins);

                return CreateModAsync(f, zip: false, loadCSharpPlugins);
            })
            .Append(Task.FromResult(CreateRysyMod()));

        if (CreateVanillaMod() is { } vanilla) {
            allMods = allMods.Append(Task.FromResult(vanilla));
            VanillaMod = vanilla;
        }

        var all = await Task.WhenAll(allMods);

        foreach (var meta in all) {
            RegisterMod(meta);
        }
    }

    private static void RegisterMod(ModMeta meta)
    {
        Filesystem.AddMod(meta);

        if (_Mods.TryGetValue(meta.Name, out var prevMod)) {
            Logger.Write("ModRegistry", LogLevel.Warning, $"Duplicate mod found: {prevMod.ToString()} [{prevMod.Filesystem.Root}] vs {meta.ToString()} [{meta.Filesystem.Root}]");
        }
        _Mods[meta.Name] = meta;
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

        RegisterMod(meta);

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

    private static ModMeta CreateRysyMod() => RysyMod = new() {
        EverestYaml = [
            new() {
                Name = "Rysy", Version = new(1, 0, 0, 0), // todo: auto-fill
            }
        ],
        PluginAssembly = typeof(RysyEngine).Assembly,
        Filesystem = RysyPlatform.Current.GetRysyFilesystem(),
    };

    private static void UnloadAllMods() {
        foreach (var (_, mod) in _Mods) {
            mod.Module?.Unload();
            mod.PluginAssembly = null;
        }
    }

    private static async Task<ModMeta> CreateModAsync(string dir, bool zip, bool loadCSharp) {
        var mod = new ModMeta();
        IModFilesystem filesystem = zip ? new ZipModFilesystem(dir.Unbackslash()) : new FolderModFilesystem(dir.Unbackslash());
        await filesystem.InitialScan();

        mod.Filesystem = filesystem;

        try {
            ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));
            if (loadCSharp)
                LoadModRysySourceCodePlugins(mod);
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

    private static void LoadModRysySourceCodePlugins(ModMeta mod, bool registerFilewatch = true) {
        #if SourceCodePlugins
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
                        RysyState.OnEndOfThisFrame += () => {
                            LoadModRysySourceCodePlugins(mod, registerFilewatch: false);
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
        
        var cachePath = $"CompileCache/{mod.Name.ToValidFilename()}";
        
        var anyFiles = CodeCompilationHelper.CompileFiles(mod.Name, fileInfo, cachePath, addGlobalUsings: !hasCsproj, out var modAsm, out var emitResult);

        if (!anyFiles)
            return;

        if (emitResult is { } && !emitResult.Success) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Warning, $"Failed compiling Rysy .cs plugins for: {mod.DisplayName}:\n{emitResult.Diagnostics.FormatDiagnostics()}");
            return;
        }

        if (emitResult is { Success: true }) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Info, $"Successfully compiled Rysy .cs plugins for: {mod.DisplayName}");
        } else if (modAsm is { }) {
            Logger.Write("Rysy Plugin Loader", LogLevel.Info, $"Successfully loaded cached Rysy .cs plugins for: {mod.DisplayName}");
        }

        mod.PluginAssembly = modAsm;

        LoadModule(mod);
        #endif
    }

    private static void ReadEverestYaml(ModMeta mod, Func<string?>? guessedNameGetter) {
        var filesystem = mod.Filesystem ?? throw new Exception($"{nameof(mod)}.{nameof(ModMeta.Filesystem)} needs to be set before calling {nameof(ReadEverestYaml)}!");

        var parsedYaml = filesystem.OpenFile("everest.yaml", ParseEverestYaml)
                      ?? filesystem.OpenFile("everest.yml", ParseEverestYaml);

        if (parsedYaml is { } && parsedYaml.Count != 0 && parsedYaml.All(m => m.IsValid())) {
            mod.EverestYaml = parsedYaml;
            return;
        }

        var guessedName = guessedNameGetter?.Invoke() ?? $"<unknown:{Guid.NewGuid()}>";
        Logger.Write("ModRegistry", LogLevel.Info, $"Found mod with no everest.yaml or an invalid one: {guessedName} [{filesystem.Root}]");
        mod.EverestYaml = [
            new() {
                Name = guessedName,
                Version = new(1, 0, 0, 0),
            }
        ];
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
