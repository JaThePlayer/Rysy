using Rysy.Helpers;

namespace Rysy.Mods;

public static class ModRegistry {
    private static Dictionary<string, ModMeta> _Mods { get; set; } = new(StringComparer.Ordinal);
    private static LayeredFilesystem LayeredFilesystem { get; set; } = new();

    public static IReadOnlyDictionary<string, ModMeta> Mods => _Mods.AsReadOnly();

    public static IModFilesystem Filesystem => LayeredFilesystem;

    /// <summary>
    /// Tries to get a <see cref="ModMeta"/> for a mod using its everest.yaml name.
    /// Returns null if the mod is not loaded.
    /// </summary>
    public static ModMeta? GetModByName(string modName) => _Mods.GetValueOrDefault(modName);

    public static async Task LoadAllAsync(string modDir) {
        _Mods.Clear();
        LayeredFilesystem = new();

        using var watch = new ScopedStopwatch("ModRegistry.LoadAll");

        var dirMods = Directory.GetDirectories(modDir).Where(dir => !dir.EndsWith("Cache")).Select(async dir => await CreateModFromDirAsync(dir));
        var zipMods = Directory.GetFiles(modDir, "*.zip").Select(async dir => await CreateModFromZipAsync(dir));
        var vanilla = CreateVanillaMod();

        var allMods = dirMods.Concat(zipMods);
        if (vanilla is { }) {
            allMods = allMods.Append(Task.FromResult(vanilla));
        }

        var all = await Task.WhenAll(allMods);

        foreach (var meta in all) {
            LayeredFilesystem.AddFilesystem(meta.Filesystem);

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

    private static async Task<ModMeta> CreateModFromDirAsync(string dir) {
        var mod = new ModMeta();
        var filesystem = new FolderModFilesystem(dir);

        await filesystem.InitialScan();

        mod.Filesystem = filesystem;

        ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));

        return mod;
    }

    private static async Task<ModMeta> CreateModFromZipAsync(string dir) {
        var mod = new ModMeta();
        var filesystem = new ZipModFilesystem(dir);
        await filesystem.InitialScan();

        mod.Filesystem = filesystem;

        ReadEverestYaml(mod, guessedNameGetter: () => Path.GetFileName(dir));

        return mod;
    }

    private static void ReadEverestYaml(ModMeta mod, Func<string?>? guessedNameGetter) {
        var filesystem = mod.Filesystem ?? throw new Exception($"{nameof(mod)}.{nameof(ModMeta.Filesystem)} needs to be set before calling {nameof(ReadEverestYaml)}!");

        var parsedYaml = filesystem.OpenFile("everest.yaml", stream => {
            using var everestYamlReader = new StreamReader(stream);
            var yamls = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata[]>(everestYamlReader);
            if (yamls.Length != 0) {
                mod.EverestYaml = yamls[0];
                return true;
            }

            return false;
        });

        if (!parsedYaml) {
            var guessedName = guessedNameGetter?.Invoke() ?? $"<unknown:{Guid.NewGuid()}>";
            Logger.Write("ModRegistry", LogLevel.Info, $"Found mod with no everest.yaml: {guessedName} [{filesystem.Root}]");
            mod.EverestYaml = new() {
                Name = guessedName,
                Version = new(1, 0, 0, 0),
            };
        }
    }
}
