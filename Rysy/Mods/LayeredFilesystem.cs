using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Mods;

public class LayeredFilesystem : IModFilesystem {
    private readonly Lock _modLock = new();
    private readonly List<ModMeta> _mods = [];
    private readonly Dictionary<string, ModMeta> _modNamesToMods = [];

    public LayeredFilesystem() {
        
    }

    public LayeredFilesystem(LayeredFilesystem toClone) {
        foreach (var mod in toClone._mods) {
            AddMod(mod);
        }
    }

    public void AddMod(ModMeta mod) {
        lock (_modLock) {
            _mods.Add(mod);
            _modNamesToMods[mod.Name] = mod;
        }
    }
    
    public void AddFilesystem(IModFilesystem modFilesystem, string name) {
        AddMod(new ModMeta { Filesystem = modFilesystem, EverestYaml = [ new() { Name = name } ]});
    }

    public string Root => "";

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        if (IsPrefixedPath(directory, out ModMeta? prefixMod, out var actualPath)) {
            foreach (var item in prefixMod.Filesystem.FindFilesInDirectoryRecursive(actualPath, extension)) {
                yield return item;
            }

            yield break;
        }
        
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return item;
            }
        }
    }
    
    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        if (IsPrefixedPath(directory, out ModMeta? prefixMod, out var actualPath)) {
            foreach (var item in prefixMod.Filesystem.FindFilesInDirectory(actualPath, extension)) {
                yield return item;
            }

            yield break;
        }
        
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectory(directory, extension)) {
                yield return item;
            }
        }
    }
    
    public IEnumerable<string> FindDirectories(string directory) {
        if (IsPrefixedPath(directory, out ModMeta? prefixMod, out var actualPath)) {
            foreach (var item in prefixMod.Filesystem.FindDirectories(actualPath)) {
                yield return item;
            }

            yield break;
        }
        
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
        if (IsPrefixedPath(directory, out ModMeta? prefixMod, out var actualPath)) {
            foreach (var item in prefixMod.Filesystem.FindFilesInDirectoryRecursive(actualPath, extension)) {
                yield return (item, prefixMod);
            }

            yield break;
        }
        
        foreach (var fs in _mods) {
            foreach (var item in fs.Filesystem.FindFilesInDirectoryRecursive(directory, extension)) {
                yield return (item, fs);
            }
        }
    }

    public IDisposable TryWatchAndOpenAll(string path, Action<Stream, ModMeta> callback, Action onChanged, Func<ModMeta, bool>? filter = null) {
        List<ModMeta> targetMods;
        if (IsPrefixedPath(path, out ModMeta? prefixMod, out var actualPath)) {
            targetMods = [prefixMod];
            path = actualPath;
        } else {
            lock (_modLock)
                targetMods = _mods
                    .Where(mod => filter?.Invoke(mod) ?? true)
                    .ToList();
        }
        
        WatchedAsset asset = new() {
            OnChanged = _ => {
                onChanged();
                InvokeAll();
            }
        };

        List<IDisposable> disposables = targetMods
            .Select(mod => mod.Filesystem.RegisterFilewatch(path, asset))
            .ToList();

        InvokeAll();

        return new ListDisposable(disposables);

        void InvokeAll() {
            foreach (var mod in targetMods) {
                if (!(filter?.Invoke(mod) ?? true))
                    continue;
            
                mod.Filesystem.TryOpenFile(path, (stream) => callback(stream, mod));
            }
        }
    }

    /// <summary>
    /// Same as TryWatchAndOpen, but the callback receives the mod the file comes form
    /// </summary>
    public bool TryWatchAndOpenWithMod(string path, Action<Stream, ModMeta> callback, 
        [NotNullWhen(true)] out IDisposable? undoWatcher) {
        undoWatcher = null;
        
        if (IsPrefixedPath(path, out var prefixMod, out var actualPath)) {
            undoWatcher = prefixMod.Filesystem.RegisterFilewatch(actualPath, new() {
                OnChanged = onChangedPath => {
                    prefixMod.Filesystem.TryOpenFile(onChangedPath, stream => {
                        callback(stream, prefixMod);
                    });
                },
            });

            return true;
        }
        
        lock (_modLock) {
            foreach (var mod in _mods) {
                if (!mod.Filesystem.TryOpenFile(path, stream => callback(stream, mod))) {
                    continue;
                }

                undoWatcher = mod.Filesystem.RegisterFilewatch(path, new() {
                    OnChanged = onChangedPath => {
                        mod.Filesystem.TryOpenFile(onChangedPath, stream => {
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
        
        if (IsPrefixedPath(filepath, out var mod, out var actualPath)) {
            return mod.Filesystem.FileExists(actualPath) ? mod : null;
        }
        
        return _mods.FirstOrDefault(m => m.Filesystem.FileExists(filepath));
    }

    public Task InitialScan() {
        return Task.WhenAll(_mods.Select(f => Task.Run(async () => await f.Filesystem.InitialScan())));
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        lock (_mods) {
            if (IsPrefixedPath(path, out var mod, out var actualPath)) {
                if (mod.Filesystem.TryOpenFile(actualPath, callback, out value))
                    return true;
            }
            
            foreach (var fs in _mods) {
                if (fs.Filesystem.TryOpenFile(path, callback, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    public IDisposable RegisterFilewatch(string path, WatchedAsset asset) {
        if (IsPrefixedPath(path, out var mod, out var actualPath)) {
            return mod.Filesystem.RegisterFilewatch(actualPath, asset);
        }
        
        List<IDisposable> watchers = [];
        foreach (var fs in _mods) {
            watchers.Add(fs.Filesystem.RegisterFilewatch(path, asset));
        }

        return new ListDisposable(watchers);
    }

    public bool FileExists(string path) {
        if (IsPrefixedPath(path, out var mod, out var actualPath)) {
            return mod.Filesystem.FileExists(actualPath);
        }

        return _mods.Any(fs => fs.Filesystem.FileExists(path));
    }

    private bool IsPrefixedPath(string path, [NotNullWhen(true)] out ModMeta? mod, [NotNullWhen(true)] out string? actualPath) {
        // modName:path/is/here
        var splitIndex = path.IndexOfAny([':', '/']);
        if (splitIndex == -1 || path[splitIndex] != ':') {
            mod = null;
            actualPath = null;
            return false;
        }

        var prefix = path.AsSpan()[..splitIndex];
        lock (_modLock) {
            if (_modNamesToMods.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(prefix, out mod)) {
                actualPath = path[(splitIndex + 1)..];
                return true;
            }
        }

        actualPath = null;
        return false;
    }
}
