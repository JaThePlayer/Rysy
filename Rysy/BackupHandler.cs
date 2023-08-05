using Rysy.Extensions;
using Rysy.Platforms;
using System.IO;
using System.Text.Json.Serialization;

namespace Rysy;

public record class BackupInfo(string MapName, DateTime Time, string BackupFilepath, string OrigFilepath) {
    [JsonIgnore]
    public Lazy<long> Filesize = new(() => File.Exists(BackupFilepath) ? new FileInfo(BackupFilepath).Length : -1);

    /*
    public static BackupInfo FromFilepath(string path) {
        var relative = Path.GetRelativePath(BackupHandler.BackupFolder, path);

        var mapName = Path.GetDirectoryName(relative)!;
        var time = File.GetLastWriteTime(path);
        
        return new(mapName, time, path) {
            Filesize = new FileInfo(path).Length,
        };
    }*/
}

public static class BackupHandler {
    public static string BackupFolder => $"{RysyPlatform.Current.GetSaveLocation()}/Backups";

    public const string IndexPath = "Backups/index.json";

    private static Dictionary<int, BackupInfo>? CachedIndex;
    private static List<BackupInfo>? CachedBackups;

    public static Dictionary<int, BackupInfo> LoadIndex() => CachedIndex ??= SettingsHelper.Load<Dictionary<int, BackupInfo>>(IndexPath, false);

    public static List<BackupInfo> GetBackups() {
        if (CachedBackups is { })
            return CachedBackups;

        var index = LoadIndex();
        return index.OrderByDescending(kv => kv.Key).Select(kv => kv.Value).ToList();
        /*
        var backupFolder = BackupFolder;
        return CachedBackups = index
            .Where(p => File.Exists(p.Value))
            .OrderByDescending(p => p.Key)
            .Select(p => BackupInfo.FromFilepath(p.Value))
            .ToList();*/
    }

    public static DateTime? GetMostRecentBackupDate() {
        var index = LoadIndex();

        var latest = index[index.Count - 1];

        if (!File.Exists(latest.BackupFilepath)) {
            return null;
        }

        return File.GetLastWriteTime(latest.BackupFilepath);
    }

    public static Map? LoadMostRecentBackup() {
        var index = LoadIndex();

        var latest = index[index.Count - 1];

        if (!File.Exists(latest.BackupFilepath)) {
            return null;
        }

        var package = BinaryPacker.FromBinary(latest.BackupFilepath);

        return Map.FromBinaryPackage(package);
    }

    public static Map? LoadBackup(BackupInfo backup) {
        var file = backup.BackupFilepath;

        if (!File.Exists(file)) {
            return null;
        }

        var package = BinaryPacker.FromBinary(file);

        return Map.FromBinaryPackage(package);
    }

    /// <summary>
    /// Makes a backup of the map, provided that the <paramref name="map"/>'s <see cref="Map.Filepath"/> is not null.
    /// Handles removing old backups as well
    /// </summary>
    public static void Backup(Map map) {
        if (map.Filepath is not { } mapFilename || !File.Exists(mapFilename)) {
            return;
        }

        // contains information about which backups exist, and their order
        var index = LoadIndex();
        var time = DateTime.Now;

        var newPath = $"{BackupFolder}/{map.Filepath.FilenameNoExt()}/{time:HH.mm.ss dd.MM.yyyy}.bin";
        newPath = Path.GetFullPath(newPath);
        
        Directory.CreateDirectory(newPath.Directory()!);

        // copy the bin into the backup folder
        File.Copy(mapFilename, newPath, true);

        // if we reached the backup cap, remove the last backup
        if (index.Count >= Settings.Instance.MaxBackups) {
            var toRemove = index[0];

            File.Delete(toRemove.BackupFilepath);
            index.Remove(0);

            // shift all indexes down by 1
            index = index.Select(p => (key: p.Key - 1, value: p.Value)).ToDictionary(p => p.key, p => p.value);
        }

        // add our new backup to the index
        index[index.Count] = new(map.Filepath.FilenameNoExt()!, time, newPath, map.Filepath);

        SettingsHelper.Save(index, IndexPath, false);
        CachedIndex = null;
        CachedBackups = null;
    }
}
