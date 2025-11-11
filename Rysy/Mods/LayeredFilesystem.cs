using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core.Tokens;

namespace Rysy.Mods;

public class LayeredFilesystem : IModFilesystem {
    private List<ModMeta> _mods = new();

    public LayeredFilesystem() {
        
    }

    public LayeredFilesystem(LayeredFilesystem toClone) {
        foreach (var mod in toClone._mods) {
            AddMod(mod);
        }
    }

    public void AddMod(ModMeta modFilesystem) {
        lock (_mods)
            _mods.Add(modFilesystem);
    }
    
    public void AddFilesystem(IModFilesystem modFilesystem, string name) {
        lock (_mods)
            _mods.Add(new ModMeta { Filesystem = modFilesystem, EverestYaml = [ new() { Name = name } ]});
    }

    public string Root => "";

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return item;
            }
        }
    }
    
    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectory(directory, extension)) {
                yield return item;
            }
        }
    }
    
    public IEnumerable<string> FindDirectories(string directory) {
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindDirectories(directory)) {
                yield return item;
            }
        }
    }

    public void NotifyFileCreated(string virtPath) {
        foreach (var m in _mods) {
            m.Filesystem.NotifyFileCreated(virtPath);
        }
    }

    /// <summary>
    /// Same as <see cref="FindFilesInDirectoryRecursive(string, string)"/>, but also returns the mod the path comes from.
    /// </summary>
    public IEnumerable<(string, ModMeta)> FindFilesInDirectoryRecursiveWithMod(string directory, string extension) {
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return (item, fs);
            }
        }
    }

    public void TryWatchAndOpenAll(string path, Action<Stream, ModMeta> callback, Action onChanged, Func<ModMeta, bool>? filter = null) {
        WatchedAsset asset = new() {
            OnChanged = (path) => {
                onChanged();
                foreach (var mod in _mods) {
                    if (!(filter?.Invoke(mod) ?? true))
                        continue;
                    
                    mod.Filesystem.TryOpenFile(path, (stream) => callback(stream, mod));
                }
            }
        };

        foreach (var mod in _mods) {
            if (!(filter?.Invoke(mod) ?? true))
                continue;
            
            mod.Filesystem.TryOpenFile(path, (stream) => callback(stream, mod));
            mod.Filesystem.RegisterFilewatch(path, asset);
        }
    }

    /// <summary>
    /// Same as TryWatchAndOpen, but the callback receives the mod the file comes form
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    public bool TryWatchAndOpenWithMod(string path, Action<Stream, ModMeta> callback) {
        lock (_mods) {
            foreach (var mod in _mods) {
                if (!mod.Filesystem.TryOpenFile(path, stream => callback(stream, mod))) {
                    continue;
                }

                mod.Filesystem.RegisterFilewatch(path, new() {
                    OnChanged = (path) => {
                        mod.Filesystem.TryOpenFile(path, stream => {
                            callback(stream, mod);
                        });
                    },
                });

                return true;
            }
        }

        return false;
    }
    
    public ModMeta? FindFirstModContaining(string? filepath) {
        if (filepath is null)
            return null;
        
        return _mods.FirstOrDefault(m => m.Filesystem.FileExists(filepath));
    }

    public Task InitialScan() {
        return Task.WhenAll(_mods.Select(f => Task.Run(async () => await f.Filesystem.InitialScan())));
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        lock (_mods) {
            foreach (var fs in _mods) {
                if (fs.Filesystem.TryOpenFile(path, callback, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        foreach (var fs in _mods) {
            if (fs.Filesystem.FileExists(path)) {
                fs.Filesystem.RegisterFilewatch(path, asset);
                return;
            }
        }
    }

    public bool FileExists(string path) {
        return _mods.Any(fs => fs.Filesystem.FileExists(path));
    }
}
