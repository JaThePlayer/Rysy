using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Rysy.Mods;

public sealed class ZipModFilesystem : IModFilesystem {
    private sealed class ZipArchiveWrapper {
        public ZipArchive Archive { get; set; }
        public bool Used { get; set; }
    }

    public string Root { get; init; }

    private readonly List<ZipArchiveWrapper> _zips = [];

    private readonly ConcurrentBag<Stream> _openedFiles = [];

    private BackgroundTaskInfo _cleanupTask;

    private readonly ConcurrentDictionary<string, List<WatchedAsset>> _watchedAssets = new(StringComparer.Ordinal);
    private readonly FileSystemWatcher _watcher;
    private readonly DelayedTaskHelper<(string Path, WatcherChangeTypes ChangeType)>? _watcherDelayedTaskHelper;
    
    // These keep track of known filenames, so that checking if a file exists in a mod incurs no IO cost.
    private volatile string[] _allEntryFullNames;
    private volatile HashSet<string> _allEntryFullNamesHashSet;

    private bool _failedToOpenZip;

    public ZipModFilesystem(string zipFilePath) {
        Root = zipFilePath;

        // setup a timer to close the zip archive if no more files from it are needed
        _cleanupTask = BackgroundTaskHelper.RegisterOnInterval(TimeSpan.FromSeconds(2), CleanupResources);
        ScanForAllEntryNames();
        
        if (!RysyPlatform.Current.SupportFileWatchers) {
            return;
        }
        
        _watcherDelayedTaskHelper = new() {
            OnDelayElapsed = HandleFileWatcherEvent,
        };
        
        _watcher = new FileSystemWatcher(zipFilePath.Directory()!.CorrectSlashes());
        _watcher.Changed += (s, e) => {
            if (e.FullPath != Root.CorrectSlashes())
                return;
            if (e.Name is null)
                return;
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            _watcherDelayedTaskHelper.Register((e.Name.Unbackslash(), e.ChangeType));
        };

        _watcher.EnableRaisingEvents = true;
    }

    private void HandleFileWatcherEvent((string path, WatcherChangeTypes changeType) args) {
        ScanForAllEntryNames();

        foreach (var file in _watchedAssets) {
            foreach (var asset in file.Value) {
                try {
                    asset.OnChanged?.Invoke(file.Key);
                } catch (Exception ex) {
                    Logger.Error(ex, $"Error when hot reloading {file.Key}");
                }
            }
        }
    }

    private void ScanForAllEntryNames() {
        var zip = OpenZipIfNeeded();
        if (zip is null)
            return;
        _allEntryFullNames = zip.Archive.Entries.Select(e => e.FullName).ToArray();
        zip.Used = false;
        _allEntryFullNamesHashSet = _allEntryFullNames.ToHashSet();
    }

    private void CleanupResources() {
        if (_zips is null)
            return;

        lock (_zips) {
            lock (_openedFiles) {
                if (!_openedFiles.IsEmpty && _openedFiles.All(x => !x.CanRead)) {
                    _openedFiles.Clear();
                }
                //if (Zips.Count > 0)
                //    Console.WriteLine($"has some zips {Root}");
                if (!_openedFiles.IsEmpty)
                    return;
                var zips = _zips;
                for (int i = zips.Count - 1; i >= 0; i--) {
                    var zip = zips[i];
                    if (!zip.Used) {
                        //Console.WriteLine($"cleared zipArchive {Root}[{i}]");
                        zip.Archive.Dispose();
                        _zips.RemoveAt(i);
                    } else {
                        //Console.WriteLine($"not cleared: {Root}[{i}]");
                    }
                }
            }
        }

    }
    private ZipArchiveWrapper? OpenZipIfNeeded() {
        lock (_zips) {
            for (int i = 0; i < _zips.Count; i++) {
                var w = _zips[i];

                if (!w.Used) {
                    w.Used = true;
                    return w;
                }
            }

            ZipArchive? zip;
            try {
#pragma warning disable CA2000
                zip = ZipFile.OpenRead(Root);
#pragma warning restore CA2000
            } catch (Exception ex) {
                if (!_failedToOpenZip)
                    Logger.Write("ZipModFilesystem", LogLevel.Warning, $"Failed to open mod zip {Root}: {ex}");
                _failedToOpenZip = true;
                return null;
            }

            _failedToOpenZip = false;

            {
                var w = new ZipArchiveWrapper {
                    Archive = zip,
                    Used = true
                };

                _zips.Add(w);

                return w;
            }
        }
    }

    public Task InitialScan() {
        return Task.CompletedTask;
    }

    
    private Stream? OpenFile(string path, ZipArchive zip) {
        var entry = zip.GetEntry(path);
        var stream = entry?.Open();
        if (stream is { }) {
            _openedFiles.Add(stream);
        }

        return stream;
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        value = default;

        // If we know the file doesn't exist, no need to open the zip
        if (!_allEntryFullNamesHashSet.Contains(path)) {
            return false;
        }
        
        var zip = OpenZipIfNeeded();
        if (zip is null) {
            return false;
        }

        using var stream = OpenFile(path, zip.Archive);
        if (stream is null) {
            value = default;
            zip.Used = false;
            return false;
        }

        value = callback(stream)!;
        zip.Used = false;

        return true;
    }

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        var files = _allEntryFullNames.SelectWhereNotNull(fullName => {
            var valid = !fullName.EndsWith('/') 
                     && fullName.StartsWith(directory, StringComparison.Ordinal)
                     && fullName.EndsWith(extension, StringComparison.Ordinal);
            return valid ? fullName : null;
        }).ToList();

        return files;
    }
    
    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        var files = _allEntryFullNames.SelectWhereNotNull(fullName => {
            var valid = !fullName.EndsWith('/') 
                        && fullName.StartsWith(directory, StringComparison.Ordinal)
                        && fullName.EndsWith(extension, StringComparison.Ordinal);
            
            if (valid && fullName.Length > directory.Length + 1 && fullName.AsSpan()[(directory.Length+1)..].Contains('/')) {
                valid = false;
            }
            
            return valid ? fullName : null;
        }).ToList();

        return files;
    }
    
    public IEnumerable<string> FindDirectories(string directory) {
        var files = _allEntryFullNames.SelectWhereNotNull(fullName => {
            var valid = fullName.EndsWith('/') && fullName.StartsWith(directory, StringComparison.Ordinal);
            
            if (valid && fullName.Length > directory.Length + 1 && fullName.AsSpan()[(directory.Length+1)..^1].Contains('/')) {
                valid = false;
            }
            
            return valid ? fullName : null;
        }).ToList();

        return files;
    }

    public IDisposable RegisterFilewatch(string path, WatchedAsset asset) {
        lock (_watchedAssets) {
            var assets = _watchedAssets.GetOrAdd(path, static _ => new(1));
            assets.Add(asset);
        }
        
        return new FilewatchDisposable(this, path, asset);
    }
    
    private class FilewatchDisposable(ZipModFilesystem fs, string path, WatchedAsset asset) : IDisposable {
        public void Dispose() {
            lock (fs._watchedAssets) {
                if (fs._watchedAssets.TryGetValue(path, out var assets)) {
                    assets.Remove(asset);
                    if (assets.Count == 0)
                        fs._watchedAssets.Remove(path, out _);
                }
            }
        }
    }
    
    public bool FileExists(string path) {
        return _allEntryFullNamesHashSet.Contains(path);
    }
    
    public void NotifyFileCreated(string virtPath) {
        _allEntryFullNamesHashSet.Add(virtPath);
    }
}
