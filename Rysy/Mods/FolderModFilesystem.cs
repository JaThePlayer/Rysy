﻿using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Rysy.Mods;

public sealed class FolderModFilesystem : IWriteableModFilesystem {
    public string Root { get; init; }

    private ConcurrentDictionary<string, List<WatchedAsset>> WatchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher Watcher;
    // keeps track of whether a file is known to exist or known not to exist in the directory.
    private readonly ConcurrentDictionary<string, bool> _knownExistingFiles = new();

    public string VirtToRealPath(string virtPath) => $"{Root}/{virtPath}";

    public FolderModFilesystem(string dirName) {
        Root = dirName;

        if (!RysyPlatform.Current.SupportFileWatchers) {
            return;
        }

        Directory.CreateDirectory(dirName);
        
        Watcher = new FileSystemWatcher(dirName.CorrectSlashes());

        FileSystemEventHandler watcherCallback = (_, e) => {
            if (e.Name is null)
                return;

            _knownExistingFiles.Clear();
            var path = e.Name.Unbackslash();

            if (WatchedAssets.TryGetValue(path, out var watched)) {
                Logger.Write(nameof(FolderModFilesystem), LogLevel.Info,
                    $"Hot reloading {path}, with {watched.Count} watchers. [{e.ChangeType}]");
                CallWatchers(path, watched, e.ChangeType);
            }

            // handle directory watchers
            foreach (var directoryWatchers in WatchedAssets.Where(w =>
                         path.StartsWith(w.Key, StringComparison.Ordinal) && path != w.Key)) {
                if (Directory.Exists(VirtToRealPath(path))) {
                    Logger.Write(nameof(FolderModFilesystem), LogLevel.Info,
                        $"Hot reloading directory {path}, with {directoryWatchers.Value.Count} watchers. [{e.ChangeType}]");
                    foreach (var item in FindFilesInDirectoryRecursive(path, "")) {
                        CallWatchers(item, directoryWatchers.Value, e.ChangeType);
                    }
                } else {
                    CallWatchers(path, directoryWatchers.Value, e.ChangeType);
                }
            }
        };
        
        Watcher.Changed += watcherCallback;
        Watcher.Deleted += watcherCallback;
        Watcher.Created += watcherCallback;
        
        Watcher.IncludeSubdirectories = true;
        Watcher.EnableRaisingEvents = true;
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
        if (string.IsNullOrWhiteSpace(path))
            return false;
        
        if (_knownExistingFiles.TryGetValue(path, out var knownResult))
            return knownResult;

        var realPath = VirtToRealPath(path);

        var exists = File.Exists(realPath);
        _knownExistingFiles[path] = exists;

        return exists;
    }

    public Stream? OpenFile(string path) {
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
        if (!Directory.Exists(realPath)) {
            return [];
        }
        
        var searchFilter = string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}";

        return Directory.EnumerateFiles(realPath, searchFilter, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }
    
    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        var realPath = VirtToRealPath(directory);
        if (!Directory.Exists(realPath)) {
            return [];
        }

        var searchFilter = string.IsNullOrWhiteSpace(extension) ? "*" : $"*.{extension}";

        return Directory.EnumerateFiles(realPath, searchFilter, SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }
    
    public IEnumerable<string> FindDirectories(string directory) {
        var realPath = VirtToRealPath(directory);
        if (!Directory.Exists(realPath)) {
            return [];
        }
        
        return Directory.EnumerateDirectories(realPath, "*", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(Root, f).Unbackslash());
    }

    public void NotifyFileCreated(string virtPath) {
        _knownExistingFiles[virtPath] = true;
    }

    public bool TryWriteToFile(string path, Action<Stream> write) {
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
        var realPath = VirtToRealPath(path);

        Directory.CreateDirectory(realPath);

        return true;
    }

    public bool TryDeleteFile(string path) {
        var realPath = VirtToRealPath(path);
        File.Delete(realPath);

        return true;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        var assets = WatchedAssets.GetOrAdd(path, static _ => new(1));

        assets.Add(asset);
    }

    public void AppendAllText(string path, string contents) {
        var realPath = VirtToRealPath(path);
        if (!realPath.StartsWith(Root, StringComparison.Ordinal))
            return;
        
        if (Path.GetDirectoryName(realPath) is {} dir)
            Directory.CreateDirectory(dir);
        
        File.AppendAllText(realPath, contents);
    }
}
