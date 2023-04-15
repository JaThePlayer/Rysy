namespace Rysy.Mods;

public class LayeredFilesystem : IModFilesystem {
    private List<ModMeta> Mods = new();

    public void AddMod(ModMeta modFilesystem) {
        lock (Mods)
            Mods.Add(modFilesystem);
    }

    public string Root => "";

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        foreach (var fs in Mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Same as <see cref="FindFilesInDirectoryRecursive(string, string)"/>, but also returns the mod the path comes from.
    /// </summary>
    public IEnumerable<(string, ModMeta)> FindFilesInDirectoryRecursiveWithMod(string directory, string extension) {
        foreach (var fs in Mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return (item, fs);
            }
        }
    }

    public ModMeta? FindFirstModContaining(string filepath) {
        return Mods.FirstOrDefault(m => m.Filesystem.FileExists(filepath));
    }

    public Task InitialScan() {
        return Task.WhenAll(Mods.Select(f => Task.Run(async () => await f.Filesystem.InitialScan())));
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value) {
        lock (Mods) {
            foreach (var fs in Mods) {
                if (fs.Filesystem.TryOpenFile(path, callback, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        foreach (var fs in Mods) {
            if (fs.Filesystem.FileExists(path)) {
                fs.Filesystem.RegisterFilewatch(path, asset);
                return;
            }
        }
    }

    public bool FileExists(string path) {
        return Mods.Any(fs => fs.Filesystem.FileExists(path));
    }
}
