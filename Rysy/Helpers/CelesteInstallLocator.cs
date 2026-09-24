using Rysy.Platforms;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rysy.Helpers;

internal class FoundProfile(string celesteDirectory, string name, ICelesteInstallLocator source) {
    public string CelesteDirectory { get; init; } = celesteDirectory;
    public string Name { get; set; } = name;
    public ICelesteInstallLocator Source { get; set; } = source;

    public override string ToString() {
        return $"FoundProfile{{CelesteDirectory={CelesteDirectory}, Name={Name}, Source={Source}}}";
    }
}

internal interface ICelesteInstallLocator {
    Task<IReadOnlyList<FoundProfile>> FindInstallsAsync();
    
    string Name { get; }
    
    int Priority { get; }
}

internal static class CelesteInstallLocator {
    /// <summary>
    /// Finds all possible Celeste installation profiles, returned profiles are guaranteed to be unique by directory name.
    /// </summary>
    public static IAsyncEnumerable<FoundProfile> FindAllInstalls() {
        ICelesteInstallLocator[] locators = [
            new ExistingProfileInstallLocator(),
            new OlympusInstallLocator(new Logger("InstallLocator.Olympus"))
        ];

        return Task.WhenEach(locators.Select(l => Task.Run(async () => await l.FindInstallsAsync())))
            .SelectMany(x => x.Result)
            .MergeByDirectory();
    }

    private static async IAsyncEnumerable<FoundProfile> MergeByDirectory(this IAsyncEnumerable<FoundProfile> from) {
        Dictionary<string, FoundProfile> foundProfiles = [];

        await foreach (var profile in from) {
            Logger.Write("CelesteInstallLocator", LogLevel.Info, $"Found profile: {profile}");
            if (foundProfiles.TryGetValue(profile.CelesteDirectory, out var previousProfile)) {
                if (profile.Source.Priority > previousProfile.Source.Priority) {
                    previousProfile.Name = profile.Name;
                    previousProfile.Source = profile.Source;
                }
            } else {
                foundProfiles[profile.CelesteDirectory] = profile;
                yield return profile;
            }
        }
    }
}

internal class ExistingProfileInstallLocator : ICelesteInstallLocator {
    public string Name => "Existing Profile";
    
    public int Priority => int.MaxValue;

    public Task<IReadOnlyList<FoundProfile>> FindInstallsAsync() {
        var known = RysyPlatform.Current.GetAllExistingProfileNames();

        return Task.FromResult<IReadOnlyList<FoundProfile>>(
            known.SelectWhereNotNull(p => Profile.Deserialize(p) is {} profile 
                    ? new FoundProfile(profile.StoredCelesteDirectory, p, this) 
                    : null)
            .ToList());
    }
}

internal class OlympusInstallLocator(IRysyLogger logger) : ICelesteInstallLocator {
    
    public string Name => "Olympus";
    
    public int Priority => 0;
    
    private string? FindOlympusStorageDir() {
        /*
         From https://github.com/EverestAPI/Olympus/blob/main/src/fs.lua#L371
        if userOS == "Windows" then
            local appdata = (allowSharp and sharp.initStatus) and sharp.getEnv("LocalAppdata"):result() or os.getenv("LocalAppData")
        return fs.joinpath(appdata, name)

        elseif userOS == "Linux" then
            return fs.joinpath(os.getenv("XDG_CONFIG_HOME") or fs.joinpath(os.getenv("HOME"), ".config"), name)

        elseif userOS == "OS X" then
            return fs.joinpath(os.getenv("HOME"), "Library", "Application Support", name)

        elseif userOS == "Android" then
            return fs.joinpath("/data/data/org.love2d.android/files/save/", "olympus")

        elseif userOS == "iOS" then
        end
         */
        const string name = "Olympus";

        if (OperatingSystem.IsWindows()) {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), name);
        }

        if (OperatingSystem.IsLinux()) {
            return Path.Combine(
                Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", ".config"),
                name);
        }

        if (OperatingSystem.IsMacOS()) {
            return Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", "Library", "Application Support", name);
        }

        if (OperatingSystem.IsAndroid()) {
            return Path.Combine("/data/data/org.love2d.android/files/save/", "olympus");
        }
        
        return null;
    }
    
    public async Task<IReadOnlyList<FoundProfile>> FindInstallsAsync() {
        string? olympusStorageDir;
        try {
            olympusStorageDir = FindOlympusStorageDir();
        } catch (Exception ex) {
            logger.Error(ex, "Failed to find Olympus config path to search for Olympus profiles.");
            return [];
        }
        
        if (olympusStorageDir is null || !Directory.Exists(olympusStorageDir)) {
            return [];
        }

        var configPath = Path.Combine(olympusStorageDir, "config.json");

        OlympusConfig? config;
        try {
            await using var file = File.OpenRead(configPath);
            config = await JsonSerializer.DeserializeAsync<OlympusConfig>(file);
        } catch (Exception ex) {
            logger.Error(ex, "Failed to deserialize Olympus config to search for Olympus profiles.");
            return [];
        }

        return config is null ? [] : config.Installs.Select(i => new FoundProfile(i.Path, i.Name, this)).ToList();
    }

    private class OlympusConfig {
        /// <summary>
        /// 1-indexed index pointing at the currently selected installation.
        /// </summary>
        [JsonPropertyName("install")]
        public int Install { get; set; }
        
        [JsonPropertyName("installs")]
        public List<Installation> Installs { get; set; }

        public Installation? GetCurrentInstall() => Installs.ElementAtOrDefault(Install - 1);
        
        public class Installation {
            [JsonPropertyName("path")]
            public string Path { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
            
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }
}

