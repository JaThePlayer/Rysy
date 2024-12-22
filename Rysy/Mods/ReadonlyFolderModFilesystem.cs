using System.Diagnostics.CodeAnalysis;

namespace Rysy.Mods;

/// <summary>
/// Wrapper over an existing filesystem, which cannot be written to, even if the underlying filesystem allows it.
/// </summary>
public sealed class ReadonlyModFilesystem(IModFilesystem filesystem) : IModFilesystem {
    public string Root => filesystem.Root;

    public Task InitialScan() {
        return filesystem.InitialScan();
    }

    public bool FileExists(string path) {
        return filesystem.FileExists(path);
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        return filesystem.TryOpenFile(path, callback, out value);
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        filesystem.RegisterFilewatch(path, asset);
    }

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        return filesystem.FindFilesInDirectoryRecursive(directory, extension);
    }

    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        return filesystem.FindFilesInDirectory(directory, extension);
    }

    public IEnumerable<string> FindDirectories(string directory) {
        return filesystem.FindDirectories(directory);
    }

    public void NotifyFileCreated(string virtPath) {
        filesystem.NotifyFileCreated(virtPath);
    }
}