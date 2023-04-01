namespace Rysy.Mods;

internal class LayeredFilesystem : IModFilesystem {
    private List<IModFilesystem> Filesystems = new();

    public void AddFilesystem(IModFilesystem modFilesystem) {
        lock (Filesystems)
            Filesystems.Add(modFilesystem);
    }

    public string Root => "";

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        foreach (var fs in Filesystems) {
            foreach (var item in fs.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return item;
            }
        }
    }

    public Task InitialScan() {
        return Task.WhenAll(Filesystems.Select(f => Task.Run(async () => await f.InitialScan())));
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value) {
        lock (Filesystems) {
            foreach (var fs in Filesystems) {
                if (fs.TryOpenFile(path, callback, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        foreach (var fs in Filesystems) {
            if (fs.FileExists(path)) {
                fs.RegisterFilewatch(path, asset);
                return;
            }
        }
    }

    public bool FileExists(string path) {
        return Filesystems.Any(fs => fs.FileExists(path));
    }
}
