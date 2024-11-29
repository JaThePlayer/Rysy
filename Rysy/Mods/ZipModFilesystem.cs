using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;

namespace Rysy.Mods;

public sealed class ZipModFilesystem : IModFilesystem {
    private sealed class ZipArchiveWrapper {
        public ZipArchive Archive { get; set; }
        public bool Used { get; set; }
    }

    public string Root { get; init; }

    private readonly List<ZipArchiveWrapper> Zips = [];

    private readonly ConcurrentBag<Stream> OpenedFiles = [];

    private BackgroundTaskInfo CleanupTask;

    private ConcurrentDictionary<string, List<WatchedAsset>> WatchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher Watcher;
    
    // These keep track of known filenames, so that checking if a file exists in a mod incurs no IO cost.
    private volatile string[] _allEntryFullNames;
    private volatile HashSet<string> _allEntryFullNamesHashSet;

    private bool _failedToOpenZip;

    public ZipModFilesystem(string zipFilePath) {
        Root = zipFilePath;

        // setup a timer to close the zip archive if no more files from it are needed
        CleanupTask = BackgroundTaskHelper.RegisterOnInterval(TimeSpan.FromSeconds(2), CleanupResources);
        ScanForAllEntryNames();
        
        if (!RysyPlatform.Current.SupportFileWatchers) {
            return;
        }
        
        Watcher = new FileSystemWatcher(zipFilePath.Directory()!.CorrectSlashes());
        Watcher.Changed += (s, e) => {
            if (e.FullPath != Root.CorrectSlashes())
                return;

            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            ScanForAllEntryNames();

            foreach (var file in WatchedAssets) {
                foreach (var asset in file.Value) {
                    /*
                    TryOpenFile(file.Key, (stream) => {
                        asset.OnChanged(stream);

                        return true;
                    }, out _);*/
                    try {
                        asset.OnChanged?.Invoke(file.Key);
                    } catch (Exception ex) {
                        Logger.Error(ex, $"Error when hot reloading {file.Key}");
                    }
                }
            }
        };

        Watcher.EnableRaisingEvents = true;
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
        if (Zips is null)
            return;

        lock (Zips) {
            lock (OpenedFiles) {
                if (!OpenedFiles.IsEmpty && OpenedFiles.All(x => !x.CanRead)) {
                    OpenedFiles.Clear();
                }
                //if (Zips.Count > 0)
                //    Console.WriteLine($"has some zips {Root}");
                if (!OpenedFiles.IsEmpty)
                    return;
                var zips = Zips;
                for (int i = zips.Count - 1; i >= 0; i--) {
                    var zip = zips[i];
                    if (!zip.Used) {
                        //Console.WriteLine($"cleared zipArchive {Root}[{i}]");
                        zip.Archive.Dispose();
                        Zips.RemoveAt(i);
                    } else {
                        //Console.WriteLine($"not cleared: {Root}[{i}]");
                    }
                }
            }
        }

    }
    private ZipArchiveWrapper? OpenZipIfNeeded() {
        lock (Zips) {
            for (int i = 0; i < Zips.Count; i++) {
                var w = Zips[i];

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

                Zips.Add(w);

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
            OpenedFiles.Add(stream);
        }

        return stream;
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value) {
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

        value = callback(stream);
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
            
            if (valid && fullName.AsSpan()[(directory.Length+1)..].Contains('/')) {
                valid = false;
            }
            
            return valid ? fullName : null;
        }).ToList();

        return files;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        lock (WatchedAssets) {
            var assets = WatchedAssets.GetOrAdd(path, static _ => new(1));
            assets.Add(asset);
        }
    }
    
    public bool FileExists(string path) {
        return _allEntryFullNamesHashSet.Contains(path);
    }
    
    public void NotifyFileCreated(string virtPath) {
        _allEntryFullNamesHashSet.Add(virtPath);
    }
}
