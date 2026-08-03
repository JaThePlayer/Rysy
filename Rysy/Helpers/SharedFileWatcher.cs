using Rysy.Platforms;

namespace Rysy.Helpers;

/// <summary>
/// Wrapper over <see cref="FileSystemWatcher"/> that can share a single OS-level watcher for multiple virtual watchers.
/// </summary>
public sealed class SharedFileWatcher : IDisposable {
    private static readonly Dictionary<string, SharedFileWatcher> SharedWatchers = [];
    private static readonly Lock SharedWatchersLock = new();

    /// <summary>
    /// Registers a watch for a given directory.
    /// Under the hood, an OS-level file watcher for a parent directory might get re-used for this watcher, but this is unnoticeable.
    /// The <see cref="FileSystemEventArgs"/> passed to the callback will have a Name relative to the passed <paramref name="directory"/>
    /// instead of the actually watched directory.
    /// <returns>A disposable which removes the watch when disposed.</returns>
    /// </summary>
    public static IDisposable RegisterWatch(string directory, Action<FileSystemEventArgs> callback) {
        directory = directory.CorrectSlashes();
        
        lock (SharedWatchersLock) {
            SharedFileWatcher? watcher = SharedWatchers.GetValueOrDefault(directory);
            string parentDirectory = directory;
            string subdirectory = "";
            while (watcher is null) {
                if (Path.GetDirectoryName(parentDirectory) is {} nextParent && !nextParent.IsNullOrWhitespace()) {
                    subdirectory = Path.Combine(Path.GetRelativePath(nextParent, parentDirectory), subdirectory);
                    parentDirectory = nextParent;
                    
                    watcher = SharedWatchers.GetValueOrDefault(parentDirectory);
                } else {
                    break;
                }
            }

            if (watcher is null) {
                watcher = SharedWatchers[directory] = new SharedFileWatcher(directory);
                subdirectory = "";
                parentDirectory = directory;
            }
            
            //Logger.Write("SharedFileWatcher", LogLevel.Debug, $"Registering watcher for {directory} via {parentDirectory}[{subdirectory}]");

            watcher.RefCount++;
            if (subdirectory != "") {
                var oldCallback = callback;
                callback = args => {
                    if (args.Name is null)
                        return;

                    if (!args.FullPath.StartsWith(directory, StringComparison.Ordinal))
                        return;
                    
                    oldCallback.Invoke(new FileSystemEventArgs(args.ChangeType, args.FullPath, args.Name.TrimPrefix(subdirectory).TrimStart(Path.DirectorySeparatorChar)));
                };
            }
            
            watcher.OnChange += callback;
            return new Scope(watcher, subdirectory, callback);
        }
    }
    
    private readonly FileSystemWatcher? _watcher;
    private readonly DelayedTaskHelper<FileSystemEventArgsEqualityByName>? _watcherDelayedTaskHelper;
    
    private SharedFileWatcher(string directory) {
        Directory = directory;
        
        if (!RysyPlatform.Current.SupportFileWatchers) {
            return;
        }
        
        _watcherDelayedTaskHelper = new() {
            OnDelayElapsed = HandleFileWatcherEvent,
        };
        
        _watcher = new FileSystemWatcher(directory.CorrectSlashes());
        
        FileSystemEventHandler watcherCallback = (_, e) => {
            if (e.Name is null)
                return;

            _watcherDelayedTaskHelper.Register(new FileSystemEventArgsEqualityByName(e));
        };
        
        _watcher.Changed += watcherCallback;
        _watcher.Deleted += watcherCallback;
        _watcher.Created += watcherCallback;

        _watcher.EnableRaisingEvents = true;
        _watcher.IncludeSubdirectories = true;
    }
    
    private readonly string Directory;

    private int RefCount;

    private event Action<FileSystemEventArgs>? OnChange;

    struct FileSystemEventArgsEqualityByName(FileSystemEventArgs args) : IEquatable<FileSystemEventArgsEqualityByName> {
        public FileSystemEventArgs Args => args;

        public bool Equals(FileSystemEventArgsEqualityByName other) {
            return Args.Name == other.Args.Name;
        }

        public override bool Equals(object? obj)
        {
            return obj is FileSystemEventArgsEqualityByName other && Equals(other);
        }

        public override int GetHashCode() {
            return Args.Name?.GetHashCode(StringComparison.Ordinal) ?? 0;
        }
    }
    
    private void HandleFileWatcherEvent(FileSystemEventArgsEqualityByName args) {
        OnChange?.Invoke(args.Args);
    }

    public void Dispose() {
        _watcher?.Dispose();
        _watcherDelayedTaskHelper?.Dispose();
    }

    private sealed class Scope(SharedFileWatcher watcher, string subdirectory, Action<FileSystemEventArgs> callback) : IDisposable {
        private bool _disposed;
        
        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;
            
            watcher.OnChange -= callback;
            lock (SharedWatchersLock) {
                watcher.RefCount--;
                if (watcher.RefCount == 0) {
                    SharedWatchers.Remove(watcher.Directory);
                    watcher.Dispose();
                    //Logger.Write("SharedFileWatcher", LogLevel.Debug, $"Disposed watcher for {watcher.Directory}.");
                }
            }
        }
    }
}