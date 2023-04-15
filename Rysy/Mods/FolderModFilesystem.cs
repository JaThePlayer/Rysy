using Rysy.Extensions;
using Rysy.History;

namespace Rysy.Mods;

public sealed class FolderModFilesystem : IModFilesystem {
    public string Root { get; init; }

    private Dictionary<string, List<WatchedAsset>> WatchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher Watcher;

    public string VirtToRealPath(string virtPath) => $"{Root}/{virtPath}";

    public FolderModFilesystem(string dirName) {
        Root = dirName;

        Watcher = new FileSystemWatcher(dirName.CorrectSlashes());
        Watcher.Changed += (s, e) => {
            if (e.Name is null)
                return;

            var path = e.Name.Unbackslash();
            if (!WatchedAssets.TryGetValue(path, out var watched)) {
                return;
            }

            foreach (var asset in watched) {
                using var stream = OpenFile(path);
                if (stream is null)
                    return;
                try {
                    asset.OnChanged(stream);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Error when hot reloading {path}");
                }
                
            }
        };
        Watcher.IncludeSubdirectories = true;
        Watcher.EnableRaisingEvents = true;
    }

    public Task InitialScan() {
        return Task.CompletedTask;
    }

    public bool FileExists(string path) {
        var realPath = VirtToRealPath(path);

        return File.Exists(realPath);
    }

    public Stream? OpenFile(string path) {
        var realPath = VirtToRealPath(path);
        if (File.Exists(realPath)) {
            return TryHelper.Try(() => File.OpenRead(realPath), retries: 3);
        }

        return null;
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value) {
        using var stream = OpenFile(path);
        if (stream is { }) {
            value = callback(stream);
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        var realPath = VirtToRealPath(directory);
        if (!Directory.Exists(realPath)) {
            return Array.Empty<string>();
        }


        var searchFilter = string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}";

        return Directory.EnumerateFiles(realPath, searchFilter, SearchOption.AllDirectories).Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        if (!WatchedAssets.TryGetValue(path, out var assets)) {
            assets = new(1);

            WatchedAssets.Add(path, assets);
        }

        assets.Add(asset);
    }
}
