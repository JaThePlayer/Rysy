using System.Diagnostics.CodeAnalysis;

namespace Rysy.Mods;

public sealed class PrefixedModFilesystem(string prefix, IModFilesystem inner) : IModFilesystem {
    public string Root => inner.Root;
    
    public Task InitialScan() {
        return inner.InitialScan();
    }
    
    private string ToInnerPath(string path) => path.TrimStart(prefix);
    
    private string ToOuterPath(string path) => prefix + path;

    private bool TryTrimPrefixFromDirectory(string directory, [NotNullWhen(true)] out string? unprefixed) {
        if (directory.StartsWith(prefix, StringComparison.Ordinal)) {
            unprefixed = directory.TrimStart(prefix);
            return true;
        }
        
        unprefixed = null;
        return false;
    }

    public bool FileExists(string path) {
        return inner.FileExists(ToInnerPath(path));
    }

    public bool TryOpenFile<T>(string path, Func<Stream, T> callback, [NotNullWhen(true)] out T? value) {
        return inner.TryOpenFile(ToInnerPath(path), callback, out value);
    }

    public void RegisterFilewatch(string path, WatchedAsset asset) {
        inner.RegisterFilewatch(ToInnerPath(path), asset);
    }

    public IEnumerable<string> FindFilesInDirectoryRecursive(string directory, string extension) {
        return TryTrimPrefixFromDirectory(directory, out var unprefixed) 
            ? inner.FindFilesInDirectoryRecursive(unprefixed, extension).Select(ToOuterPath)
            : [];
    }

    public IEnumerable<string> FindFilesInDirectory(string directory, string extension) {
        return TryTrimPrefixFromDirectory(directory, out var unprefixed) 
            ? inner.FindFilesInDirectory(unprefixed, extension).Select(ToOuterPath)
            : [];
    }

    public IEnumerable<string> FindDirectories(string directory) {
        return TryTrimPrefixFromDirectory(directory, out var unprefixed)
            ? inner.FindDirectories(unprefixed).Select(ToOuterPath)
            : [];
    }

    public void NotifyFileCreated(string virtPath) {
        inner.NotifyFileCreated(ToInnerPath(virtPath));
    }
}