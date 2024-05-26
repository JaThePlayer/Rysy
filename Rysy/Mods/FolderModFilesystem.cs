﻿using Rysy.Extensions;
using Rysy.Helpers;
using System.Collections.Concurrent;
using System.IO;

namespace Rysy.Mods;

public sealed class FolderModFilesystem : IWriteableModFilesystem {
    public string Root { get; init; }

    private Dictionary<string, List<WatchedAsset>> WatchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher Watcher;
    // keeps track of whether a file is known to exist or known not to exist in the directory.
    private readonly ConcurrentDictionary<string, bool> _knownExistingFiles = new();

    public string VirtToRealPath(string virtPath) => $"{Root}/{virtPath}";

    public FolderModFilesystem(string dirName) {
        Root = dirName;

        Watcher = new FileSystemWatcher(dirName.CorrectSlashes());
        Watcher.Changed += (s, e) => {
            if (e.Name is null)
                return;
            
            _knownExistingFiles.Clear();
            //e.LogAsJson();
            var path = e.Name.Unbackslash();
            switch (e.ChangeType) {
                case WatcherChangeTypes.Created: {
                    break;
                }
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed: {
                    //WatchedAssets.Keys.LogAsJson();
                    /*
                    if (!WatchedAssets.TryGetValue(path, out var watched)) {
                        // handle cases where you're editing a folder - all files in that folder need to be updated
                        if (WatchedAssets.FirstOrDefault(w => !Path.GetRelativePath(relativeTo: w.Key, path).StartsWith("..")) is not { Value: { } } watchedDir)
                            return;
                        Console.WriteLine(watchedDir.Key);
                        watched ??= watchedDir.Value;
                    }

                    CallWatchers(path, watched);*/
                    if (WatchedAssets.TryGetValue(path, out var watched)) {
                        CallWatchers(path, watched);
                    }

                    // handle directory watchers
                    foreach (var directoryWatchers in WatchedAssets.Where(w => path.StartsWith(w.Key, StringComparison.Ordinal) && path != w.Key)) {
                        if (Directory.Exists(VirtToRealPath(path))) {
                            foreach (var item in FindFilesInDirectoryRecursive(path, "")) {
                                CallWatchers(item, directoryWatchers.Value);
                            }
                        } else {
                            CallWatchers(path, directoryWatchers.Value);
                        }

                    }

                    break;
                }
                case WatcherChangeTypes.Renamed:
                    break;
                default:
                    break;
            }
        };
        Watcher.IncludeSubdirectories = true;
        Watcher.EnableRaisingEvents = true;
    }

    private void CallWatchers(string path, List<WatchedAsset>? watched) {
        RysyState.OnEndOfThisFrame += () => {
            foreach (var asset in watched?.ToList() ?? new()) {
                /*
                using var stream = OpenFile(path);
                if (stream is null)
                    return;
                try {
                    asset.OnChanged?.Invoke(stream);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Error when hot reloading {path}");
                }*/
                Logger.Write(nameof(FolderModFilesystem), LogLevel.Info, $"Hot reloading {path}");
                try {
                    asset.OnChanged?.Invoke(path);
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

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value) {
        ArgumentNullException.ThrowIfNull(callback);

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

    public bool TryWriteToFile(string path, Action<Stream> write) {
        var realPath = VirtToRealPath(path);

        if (!realPath.StartsWith(Root))
            return false;
        
        if (Path.GetDirectoryName(realPath) is {} dir)
            Directory.CreateDirectory(dir);
        
        using var fileStream = File.Open(realPath, FileMode.Create);

        write(fileStream);

        return true;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        if (!WatchedAssets.TryGetValue(path, out var assets)) {
            assets = new(1);

            WatchedAssets.Add(path, assets);
        }

        assets.Add(asset);
    }
}
