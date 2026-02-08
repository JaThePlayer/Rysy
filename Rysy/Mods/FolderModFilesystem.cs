using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Rysy.Mods;

public sealed class FolderModFilesystem : IWriteableModFilesystem {
    public string Root { get; init; }

    private ConcurrentDictionary<string, List<WatchedAsset>> _watchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher _watcher;
    // keeps track of whether a file is known to exist or known not to exist in the directory.
    private readonly ConcurrentDictionary<string, bool> _knownExistingFiles = new();

    public string VirtToRealPath(string virtPath) => $"{Root}/{virtPath}";

    private readonly DelayedTaskHelper<(string Path, WatcherChangeTypes ChangeType)>? _watcherDelayedTaskHelper;

    private readonly bool _valid;

    public FolderModFilesystem(string dirName) {
        Root = dirName;

        try {
            Directory.CreateDirectory(dirName);
        } catch (IOException ex) {
            Logger.Error("FolderModFilesystem", ex, $"Failed to create FolderModFilesystem for directory: {dirName}");
            return;
        }

        _valid = true;
        
        if (!RysyPlatform.Current.SupportFileWatchers) {
            return;
        }
        
        _watcherDelayedTaskHelper = new() {
            OnDelayElapsed = HandleFileWatcherEvent,
        };
        
        _watcher = new FileSystemWatcher(dirName.CorrectSlashes());

        FileSystemEventHandler watcherCallback = (_, e) => {
            if (e.Name is null)
                return;

            _knownExistingFiles.Clear();
            _watcherDelayedTaskHelper.Register((e.Name.Unbackslash(), e.ChangeType));
        };
        
        _watcher.Changed += watcherCallback;
        _watcher.Deleted += watcherCallback;
        _watcher.Created += watcherCallback;
        
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
    }

    private void HandleFileWatcherEvent((string path, WatcherChangeTypes changeType) args) {
        var (path, changeType) = args;
        
        if (_watchedAssets.TryGetValue(path, out var watched)) {
            Logger.Write(nameof(FolderModFilesystem), LogLevel.Info,
                $"Hot reloading {path}, with {watched.Count} watchers. [{changeType}]");
            CallWatchers(path, watched, changeType);
        }

        // handle directory watchers
        foreach (var directoryWatchers in _watchedAssets.Where(w =>
                     path.StartsWith(w.Key, StringComparison.Ordinal) && path != w.Key)) {
            if (Directory.Exists(VirtToRealPath(path))) {
                Logger.Write(nameof(FolderModFilesystem), LogLevel.Info,
                    $"Hot reloading directory {path}, with {directoryWatchers.Value.Count} watchers. [{changeType}]");
                foreach (var item in FindFilesInDirectoryRecursive(path, "")) {
                    CallWatchers(item, directoryWatchers.Value, changeType);
                }
            } else {
                CallWatchers(path, directoryWatchers.Value, changeType);
            }
        }
    }

    private void CallWatchers(string path, List<WatchedAsset>? watched, WatcherChangeTypes type) {
        if (watched is null)
            return;
        
        RysyState.OnEndOfThisFrame += () => {
            foreach (var asset in watched?.ToList() ?? new()) {
                try {
                    switch (type) {
                        case WatcherChangeTypes.Changed:
                            asset.OnChanged?.Invoke(path);
                            break;
                        case WatcherChangeTypes.Deleted:
                            asset.OnRemoved?.Invoke(path);
                            break;
                        case WatcherChangeTypes.Created:
                            asset.OnCreated?.Invoke(path);
                            break;
                    }
                    
                } catch (Exception ex) {
                    Logger.Error(ex, $"Error when hot reloading {path}");
                }
            }
        };
    }

    public Task InitialScan() {
        return Task.CompletedTask;
    }

    public bool FileExists(string path) {
        if (!_valid || string.IsNullOrWhiteSpace(path))
            return false;
        
        if (_knownExistingFiles.TryGetValue(path, out var knownResult))
            return knownResult;

        var realPath = VirtToRealPath(path);

        var exists = File.Exists(realPath);
        _knownExistingFiles[path] = exists;

        return exists;
    }

    public Stream? OpenFile(string path) {
        if (!_valid)
            return null;
        
        var realPath = VirtToRealPath(path);
        if (File.Exists(realPath)) {
            return TryHelper.Try(() => File.OpenRead(realPath), retries: 3);
        }

        return null;
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        ArgumentNullException.ThrowIfNull(callback);

        using var stream = OpenFile(path);
        if (stream is { }) {
            value = callback(stream)!;
            return true;
        }

        value = default;
        return false;
    }

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        var realPath = VirtToRealPath(directory);
        if (!_valid || !Directory.Exists(realPath)) {
            return [];
        }
        
        var searchFilter = string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}";

        return Directory.EnumerateFiles(realPath, searchFilter, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }
    
    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        var realPath = VirtToRealPath(directory);
        if (!_valid || !Directory.Exists(realPath)) {
            return [];
        }

        var searchFilter = string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}";

        return Directory.EnumerateFiles(realPath, searchFilter, SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }
    
    public IEnumerable<string> FindDirectories(string directory) {
        var realPath = VirtToRealPath(directory);
        if (!_valid || !Directory.Exists(realPath)) {
            return [];
        }
        
        return Directory.EnumerateDirectories(realPath, "*", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }

    public void NotifyFileCreated(string virtPath) {
        _knownExistingFiles[virtPath] = true;
    }

    public bool TryWriteToFile(string path, Action<Stream> write) {
        if (!_valid)
            return false;

        var realPath = VirtToRealPath(path);

        if (!realPath.StartsWith(Root, StringComparison.Ordinal))
            return false;
        
        if (Path.GetDirectoryName(realPath) is {} dir)
            Directory.CreateDirectory(dir);
        
        using var fileStream = File.Open(realPath, FileMode.Create);

        write(fileStream);
        
        NotifyFileCreated(path);

        return true;
    }

    public bool TryCreateDirectory(string path) {
        if (!_valid)
            return false;

        var realPath = VirtToRealPath(path);

        Directory.CreateDirectory(realPath);

        return true;
    }

    public bool TryDeleteFile(string path) {
        if (!_valid)
            return false;

        var realPath = VirtToRealPath(path);
        File.Delete(realPath);

        return true;
    }

    public IDisposable RegisterFilewatch(string path, WatchedAsset asset) {
        var assets = _watchedAssets.GetOrAdd(path, static _ => new(1));

        assets.Add(asset);
        return new FilewatchDisposable(this, path, asset);
    }

    private class FilewatchDisposable(FolderModFilesystem fs, string path, WatchedAsset asset) : IDisposable {
        public void Dispose() {
            if (fs._watchedAssets.TryGetValue(path, out var assets)) {
                assets.Remove(asset);
                if (assets.Count == 0)
                    fs._watchedAssets.Remove(path, out _);
            }
        }
    }

    public void AppendAllText(string path, string contents) {
        if (!_valid)
            return;
        
        var realPath = VirtToRealPath(path);
        if (!realPath.StartsWith(Root, StringComparison.Ordinal))
            return;
        
        if (Path.GetDirectoryName(realPath) is {} dir)
            Directory.CreateDirectory(dir);
        
        File.AppendAllText(realPath, contents);
    }
}
