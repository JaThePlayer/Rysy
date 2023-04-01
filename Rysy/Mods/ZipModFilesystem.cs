using Rysy.Extensions;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace Rysy.Mods;

public sealed class ZipModFilesystem : IModFilesystem {
    private class ZipArchiveWrapper {
        public ZipArchive Archive { get; set; }
        public bool Used { get; set; }
    }

    public string Root { get; init; }

    private List<ZipArchiveWrapper> Zips = new();

    private ConcurrentBag<Stream> OpenedFiles = new();

    private CancellationTokenSource Token;

    private Dictionary<string, List<WatchedAsset>> WatchedAssets = new(StringComparer.Ordinal);
    private FileSystemWatcher Watcher;


    public ZipModFilesystem(string zipFilePath) {
        Root = zipFilePath;

        Token = new();
        var token = Token.Token;

        // setup a timer to close the zip archive if no more files from it are needed
        Task.Run(async () => {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            while (await timer.WaitForNextTickAsync(token)) {
                CleanupResources();
            }
        }, token);

        Watcher = new FileSystemWatcher(zipFilePath.Directory()!.CorrectSlashes());
        Watcher.Changed += (s, e) => {
            if (e.FullPath != Root.CorrectSlashes())
                return;
            e.LogAsJson();

            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            foreach (var file in WatchedAssets) {
                foreach (var asset in file.Value) {
                    TryOpenFile(file.Key, (stream) => {
                        asset.OnChanged(stream);

                        return true;
                    }, out _);
                }
            }
        };

        Watcher.EnableRaisingEvents = true;
    }

    private void CleanupResources() {
        if (Zips is null)
            return;

        /*
        lock (Zips) {
            lock (OpenedFiles) {
                if (!OpenedFiles.IsEmpty && OpenedFiles.All(x => !x.CanRead)) {
                    OpenedFiles.Clear();
                    foreach (var wrapper in Zips) {
                        wrapper.Archive.Dispose();
                    }

                    Zips.Clear();
                }
            }
        }*/

        lock (Zips) {
            lock (OpenedFiles) {
                if (!OpenedFiles.IsEmpty && OpenedFiles.All(x => !x.CanRead)) {
                    OpenedFiles.Clear();
                }

                if (!OpenedFiles.IsEmpty)
                    return;
                    
                var zips = Zips;
                for (int i = zips.Count - 1; i >= 0; i--) {
                    var zip = zips[i];
                    if (!zip.Used) {
                        //Console.WriteLine("cleared zipArchive");
                        zip.Archive.Dispose();
                        Zips.RemoveAt(i);
                    } else {
                        Console.WriteLine($"not cleared: {Root}[{i}]");
                    }
                }
            }
        }

    }
    private ZipArchiveWrapper OpenZipIfNeeded() {
        lock (Zips) {
            for (int i = 0; i < Zips.Count; i++) {
                var w = Zips[i];

                if (!w.Used) {
                    w.Used = true;
                    return w;
                }
            }

            {
                var zip = ZipFile.OpenRead(Root);
                var w = new ZipArchiveWrapper() {
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
        var zip = OpenZipIfNeeded();

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
        var zip = OpenZipIfNeeded();

        var files = zip.Archive.Entries.SelectWhereNotNull(e => {
            var fullName = e.FullName;
            var valid = fullName.StartsWith(directory, StringComparison.Ordinal)
                     && fullName.EndsWith(extension, StringComparison.Ordinal);
            return valid ? fullName : null;
        }).ToList();

        zip.Used = false;

        return files;
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        if (!WatchedAssets.TryGetValue(path, out var assets)) {
            assets = new(1);

            WatchedAssets.Add(path, assets);
        }

        assets.Add(asset);
    }

    public bool FileExists(string path) {
        var zip = OpenZipIfNeeded();

        var exists = zip.Archive.GetEntry(path) is { };

        zip.Used = false;

        return exists;
    }
}
