namespace Rysy.Mods;

/// <summary>
/// Provides methods for accessing files for a given mod, regardless of it being in a zip or folder.
/// </summary>
public interface IModFilesystem {
    /// <summary>
    /// Gets the root folder/zip name for this filesystem
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Performs an initial scan of the filesystem, if needed
    /// </summary>
    public Task InitialScan();

    /// <summary>
    /// Checks whether a file at the given path exists.
    /// </summary>
    public bool FileExists(string path);

    /// <summary>
    /// Tries to open a file at a given virtual path, which includes the file extension.
    /// If the file was found, calls <paramref name="callback"/> with the stream for that file, then returns true and <paramref name="value"/> gets set to the return value of the callback.
    /// If not, the function returns false.
    /// DO NOT capture the <see cref="Stream"/> received in the callback, as it might get disposed as soon as this method finishes.
    /// </summary>
    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, out T? value);

    /// <summary>
    /// Registers a file watcher for the given virtual path.
    /// Various callbacks from the <paramref name="asset"/> will be called when this file changes.
    /// </summary>
    public void RegisterFilewatch(string path, WatchedAsset asset);

    /// <summary>
    /// Finds all files that are contained in the <paramref name="directory"/> with the file extension <paramref name="extension"/>.
    /// Returned paths use forward slashes, and contain the file extension.
    /// If extension is an empty string, extensions are ignored.
    /// Calling OpenFile(path) using paths returned by this function allows you to access the file.
    /// </summary>
    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension);

    /// <summary>
    /// Notifies the filesystem that a file at the given virtual path just got created.
    /// </summary>
    public void NotifyFileCreated(string virtPath);
}

public interface IWriteableModFilesystem : IModFilesystem {
    public bool TryWriteToFile(string path, Action<Stream> write);

    public bool TryCreateDirectory(string path);
}

public static class WriteableModFilesystemExt {
    public static bool TryWriteToFile(this IWriteableModFilesystem fs, string path, Stream toWrite) {
        return fs.TryWriteToFile(path, toWrite.CopyTo);
    }
    
    public static bool TryWriteToFile(this IWriteableModFilesystem fs, string path, string fileContents) {
        return fs.TryWriteToFile(path, s => {
            using var writer = new StreamWriter(s);
            
            writer.Write(fileContents);
        });
    }
    
    public static bool CopyFileTo(this IWriteableModFilesystem fs, string from, string to) {
        return fs.OpenFile(from, fromStr => fs.TryWriteToFile(to, fromStr.CopyTo));
    }
}

public sealed class WatchedAsset {
    public Action<string>? OnChanged { get; set; }
}


public static class ModFilesystemExtensions {
    /// <summary>
    /// Reads the given file as a string.
    /// </summary>
    public static string? TryReadAllText(this IModFilesystem filesystem, string path) {
        return filesystem.OpenFile(path, stream => {
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        });
    }

    /// <summary>
    /// Reads the given file.
    /// </summary>
    public static byte[]? TryReadAllBytes(this IModFilesystem filesystem, string path) {
        return filesystem.OpenFile(path, stream => {
            using MemoryStream ms = new MemoryStream();

            stream.CopyTo(ms);
            return ms.ToArray();
        });
    }

    /// <summary>
    /// Tries to open a file at a given virtual path, which includes the file extension.
    /// If the file was found, calls <paramref name="callback"/> with the stream for that file, and returns the value returned by the callback.
    /// If not, the default value for the given type is returned, and the callback doesn't get called.
    /// DO NOT capture the <see cref="Stream"/> received in the callback, as it might get disposed as soon as this method finishes.
    /// </summary>
    public static T? OpenFile<T>(this IModFilesystem filesystem, string path, Func<Stream, T> callback) {
        if (filesystem.TryOpenFile(path, callback, out var value)) {
            return value;
        }

        return default;
    }

    /// <summary>
    /// Tries to open a file at a given virtual path, which includes the file extension.
    /// If the file was found, calls <paramref name="callback"/> with the stream for that file.
    /// If not, the callback doesn't get called.
    /// DO NOT capture the <see cref="Stream"/> received in the callback, as it might get disposed as soon as this method finishes.
    /// </summary>
    public static bool TryOpenFile(this IModFilesystem filesystem, string path, Action<Stream> callback) {
        return filesystem.TryOpenFile(path, (stream) => {
            callback(stream);
            return true;
        }, out _);
    }


    /// <summary>
    /// Tries to open the file at <paramref name="path"/>, calling the <paramref name="callback"/>.
    /// If the file exists, also sets up a file watcher which will call the <paramref name="callback"/> whenever the file changes.
    /// If the file doesn't exist, the <paramref name="callback"/> never gets called and this returns false.
    /// </summary>
    public static bool TryWatchAndOpen(this IModFilesystem filesystem, string path, Action<Stream> callback) {
        if (filesystem.TryOpenFile(path, callback)) {
            filesystem.RegisterFilewatch(path, new() {
                OnChanged = (path) => {
                    filesystem.TryOpenFile(path, stream => {
                        callback(stream);
                    });
                },
            });

            return true;
        }

        return false;
    }
}
