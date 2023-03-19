using Rysy.Extensions;
using Rysy.Helpers;
using System.IO;
using System.IO.Compression;

namespace Rysy;

public static class ModAssetHelper {
    private static Dictionary<string, FileSystemWatcher> FileWatchers = new();

    /// <summary>
    /// Opens a file from a mod at <paramref name="relativePath"/>.
    /// This function doesn't know which mod the file is in, so it'll look at all mods until it finds it.
    /// </summary>
    public static Stream? OpenModFile(string relativePath) {
        (string? dirPath, ZipArchiveEntry? zipEntry, string? zipPath) = SearchModFile(relativePath);

        if (dirPath is { }) {
            return File.OpenRead(dirPath);
        }
        if (zipEntry is { }) {
            return zipEntry.Open();
        }

        return null;
    }

    public static Cache<Stream?>? GetModFileCache(string relativePath) {
        (string? dirPath, ZipArchiveEntry? zipEntry, string? zipPath) = SearchModFile(relativePath);

        var token = new CacheToken();
        token.Reset();

        //TODO: consolidate this, this is 99% copy pasted!
        if (dirPath is { }) {
            var correctPath = dirPath.CorrectSlashes();
            var listener = new FileSystemWatcher(Path.GetDirectoryName(correctPath)!);
            SetupListener(token, correctPath, listener);
            SetupTokenDispose(relativePath, token, listener);
            FileWatchers[relativePath] = listener;

            return token.CreateCache<Stream?>(() => {
                for (int i = 0; i < 3; i++) {
                    try {
                        return File.OpenRead(dirPath);
                    } catch {
                        Logger.Write("ModAssetHelper", LogLevel.Warning, $"Edited file {correctPath} locked, waiting...");
                        Thread.Sleep(500 * i);
                    }
                }

                return null;
            });
        }

        if (zipEntry is { } && zipPath is { }) {
            var correctPath = zipPath.CorrectSlashes();
            var listener = new FileSystemWatcher(Path.GetDirectoryName(correctPath)!);
            SetupListener(token, correctPath, listener);
            SetupTokenDispose(relativePath, token, listener);
            FileWatchers[relativePath] = listener;

            return token.CreateCache<Stream?>(() => {
                for (int i = 0; i < 3; i++) {
                    try {
                        return ZipFile.OpenRead(zipPath).GetEntry(relativePath)?.Open();
                    } catch {
                        Logger.Write("ModAssetHelper", LogLevel.Warning, $"Edited file {correctPath} locked, waiting...");
                        Thread.Sleep(500 * i);
                    }
                }

                return null;
            });
        }

        return null;
    }

    private static void SetupTokenDispose(string relativePath, CacheToken token, FileSystemWatcher listener) {
        token.OnDispose += () => {
            listener.Dispose();

            if (FileWatchers.TryGetValue(relativePath, out var storedWatcher) && storedWatcher == listener) {
                FileWatchers.Remove(relativePath);
            }
        };
    }

    private static void SetupListener(CacheToken token, string correctPath, FileSystemWatcher listener) {
        listener.Changed += (s, e) => {
            if (e.FullPath == correctPath) {
                token.Invalidate();
            } 
        };

        listener.EnableRaisingEvents = true;
    }

    private static (string? dirPath, ZipArchiveEntry? zipEntry, string? zipPath) SearchModFile(string relativePath) {
        foreach (var dir in Directory.GetDirectories(Profile.Instance.ModsDirectory)) {
            var path = $"{dir}/{relativePath}";
            if (File.Exists(path)) {
                return (path, null, null);
            }
        }

        foreach (var zip in Directory.EnumerateFiles(Profile.Instance.ModsDirectory, "*.zip")) {
            var arch = ZipFile.OpenRead(zip);
            if (arch.GetEntry(relativePath) is { } entry) {
                return (null, entry, zip);
            }
        }

        return (null, null, null);
    }
}