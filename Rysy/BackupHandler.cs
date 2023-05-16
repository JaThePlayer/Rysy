using Rysy.Extensions;
using Rysy.Platforms;

namespace Rysy;

public record class BackupInfo(string MapName, DateTime Time, string Filepath, long Filesize) {
    public static BackupInfo FromFilepath(string path) {
        var relative = Path.GetRelativePath(BackupHandler.BackupFolder, path);

        var mapName = Path.GetDirectoryName(relative)!;
        var time = File.GetLastWriteTime(path);
        
        return new(mapName, time, path, new FileInfo(path).Length);
    }
}

public static class BackupHandler {
    public static string BackupFolder => $"{RysyPlatform.Current.GetSaveLocation()}/Backups";

    public const string IndexPath = "Backups/index.json";

    public static Dictionary<int, string> LoadIndex() => SettingsHelper.Load<Dictionary<int, string>>(IndexPath, false);

    public static List<BackupInfo> GetBackups() {
        var index = LoadIndex();
        var backupFolder = BackupFolder;

        return index
            .Where(p => File.Exists(p.Value))
            .OrderByDescending(p => p.Key)
            .Select(p => BackupInfo.FromFilepath(p.Value))
            .ToList();
    }

    public static DateTime? GetMostRecentBackupDate() {
        var index = LoadIndex();

        var latest = index[index.Count - 1];

        if (!File.Exists(latest)) {
            return null;
        }

        return File.GetLastWriteTime(latest);
    }

    public static Map? LoadMostRecentBackup() {
        var index = LoadIndex();

        var latest = index[index.Count - 1];

        if (!File.Exists(latest)) {
            return null;
        }

        var package = BinaryPacker.FromBinary(latest);

        return Map.FromBinaryPackage(package);
    }

    public static Map? LoadBackup(BackupInfo backup) {
        var file = backup.Filepath;

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

        var newPath = $"{BackupFolder}/{map.Filepath.FilenameNoExt()}/{DateTime.Now:HH.mm.ss dd.MM.yyyy}.bin";
        newPath = Path.GetFullPath(newPath);
        
        Directory.CreateDirectory(newPath.Directory()!);

        // copy the bin into the backup folder
        File.Copy(mapFilename, newPath, true);

        // if we reached the backup cap, remove the last backup
        if (index.Count >= Settings.Instance.MaxBackups) {
            var toRemove = index[0];

            File.Delete(toRemove);
            index.Remove(0);

            // shift all indexes down by 1
            index = index.Select(p => (key: p.Key - 1, value: p.Value)).ToDictionary(p => p.key, p => p.value);
        }

        // add our new backup to the index
        index[index.Count] = newPath;

        SettingsHelper.Save(index, IndexPath, false);
    }
}
