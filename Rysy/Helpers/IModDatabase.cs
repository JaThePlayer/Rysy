// ReSharper disable InconsistentNaming
namespace Rysy.Helpers;

public interface IModDatabase {
    public Task<Dictionary<string, DatabaseModInfo>> GetKnownModsAsync();

    public static IModDatabase DefaultDatabase { get; } = new MaddieModDatabase();
}

public sealed class DatabaseModInfo {
    public string GameBananaType { get; set; }

    public string Version { get; set; }

    public long LastUpdate { get; set; }

    public long Size { get; set; }

    public long GameBananaId { get; set; }

    public long GameBananaFileId { get; set; }

    public string[] xxHash { get; set; }

    public Uri URL { get; set; }
}

sealed class MaddieModDatabase : IModDatabase {
    private static readonly Uri Uri = new(@"https://maddie480.ovh/celeste/everest_update.yaml");

    private readonly Lazy<Task<Dictionary<string, DatabaseModInfo>>> _mods;

    public MaddieModDatabase() {
        _mods = new(GetKnownModsAsyncImpl, LazyThreadSafetyMode.ExecutionAndPublication) {};
    }
    
    public Task<Dictionary<string, DatabaseModInfo>> GetKnownModsAsync() {
        return _mods.Value;
    }
    
    private async Task<Dictionary<string, DatabaseModInfo>> GetKnownModsAsyncImpl() {
        try {
            Logger.Write("MaddieModDatabase", LogLevel.Info, $"Getting mod list from {Uri}");
            using var client = new HttpClient();

            await using var stream = await client.GetStreamAsync(Uri);
            using var reader = new StreamReader(stream);

            var mods = YamlHelper.Deserializer.Deserialize<Dictionary<string, DatabaseModInfo>>(reader);
        
            Logger.Write("MaddieModDatabase", LogLevel.Info, $"Successfully got mod list from {Uri}. Received {mods.Count} mods.");
            return mods;
        } catch (Exception e) {
            Logger.Write("MaddieModDatabase", LogLevel.Error, $"Failed to get everest_update.yaml from {Uri}: {e}");
            return [];
        }
    }
}