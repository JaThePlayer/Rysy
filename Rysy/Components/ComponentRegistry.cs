using Rysy.Components;
using Rysy.Helpers;
using Rysy.Signals;
using System.Collections;

namespace Rysy;

public interface IComponentRegistry : ISignalListener
{
    void Add<T>(T component) where T : notnull;
    void Remove<T>(T component) where T : notnull;
    T? Get<T>() where T : class;
    IReadOnlyList<T> GetAll<T>() where T : class;
    
    /// <summary>
    /// Gets all components castable to the <paramref name="targetType"/> argument, cast to the generic type.<br/>
    /// <paramref name="targetType"/> should extend from <typeparamref name="T"/>.
    /// </summary>
    IReadOnlyList<T> GetAll<T>(Type targetType) where T : class;

    IEnumerable<object> GetAll();

    /// <summary>
    /// Locks all changes to the registry until the returned <see cref="IDisposable"/> is disposed.
    /// Changes done during that time will get queued until the registry is unlocked.<br/>
    ///
    /// Nesting locks is supported, but they have to be disposed in reverse order.
    /// </summary>
    IDisposable LockChanges();

    void ISignalListener.OnSignal<T>(T signal) {
        foreach (var listener in GetAll<ISignalListener>()) {
            listener.OnSignal(signal);
        }
            
        foreach (var listener in GetAll<ISignalListener<T>>()) {
            listener.OnSignal(signal);
        }
    }
}

public static class ComponentRegistryExt {
    extension(IComponentRegistry registry) {
        public T GetRequired<T>() where T : class {
            return registry.Get<T>() ?? throw new RequiredComponentMissingException(typeof(T));;
        }
        
        public T AddIfMissing<T>() where T : class, new() {
            if (registry.Get<T>() is not { } ret) {
                registry.Add(ret = new T());
            }

            return ret;
        }

        public T AddIfMissing<T>(T newValue) where T : class {
            if (registry.Get<T>() is not { } ret) {
                registry.Add(ret = newValue);
            }

            return ret;
        }
        
        public void OnSignal<T>(T signal) where T : ISignal {
            registry.OnSignal(signal);
        }

        /// <summary>
        /// Enumerate through all results of <see cref="IComponentRegistry.GetAll{T}()"/>,
        /// while locking the registry until the enumerator is disposed.
        /// </summary>
        public EnumerateAllLockedEnumerable<T> EnumerateAllLocked<T>() where T : class {
            return new EnumerateAllLockedEnumerable<T>(registry);
        }
    }

    public struct EnumerateAllLockedEnumerable<T>(IComponentRegistry registry) : IEnumerable<T>, IEnumerator<T> where T : class {
        public EnumerateAllLockedEnumerable<T> GetEnumerator() {
            if (_inner is null) {
                Init();
                return this;
            }

            var other = new EnumerateAllLockedEnumerable<T>(registry);
            other.Init();
            return other;
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private IEnumerator<T> _inner;
        private IDisposable _lock;

        private void Init() {
            _inner = registry.GetAll<T>().GetEnumerator();
            _lock = registry.LockChanges();
        }
        
        public void Dispose() {
            _lock.Dispose();
        }

        public bool MoveNext() {
            return _inner.MoveNext();
        }

        public void Reset() {
            throw new InvalidOperationException();
        }

        public T Current => _inner.Current;

        object IEnumerator.Current => _inner.Current;
    }
}

public sealed class ComponentRegistryScope(IComponentRegistry parent) : IComponentRegistry, IDisposable {
    private List<object> _newComponents = [];
    
    public void Dispose() {
        foreach (var toRemove in _newComponents) {
            parent.Remove(toRemove);
        }
        
        _newComponents.Clear();
    }

    public void Add<T>(T component) where T : notnull {
        _newComponents.Add(component);
        parent.Add(component);
    }

    public void Remove<T>(T component) where T : notnull {
        _newComponents.Remove(component);
        parent.Remove(component);
    }

    public T? Get<T>() where T : class {
        return parent.Get<T>();
    }

    public IReadOnlyList<T> GetAll<T>() where T : class {
        return parent.GetAll<T>();
    }
    
    public IReadOnlyList<T> GetAll<T>(Type targetType) where T : class {
        return parent.GetAll<T>(targetType);
    }

    public IEnumerable<object> GetAll() {
        return parent.GetAll();
    }

    public IDisposable LockChanges() {
        return parent.LockChanges();
    }
}

public sealed class ComponentRegistry : IComponentRegistry {
    private TypeTrackedList<object> Components { get; } = [];

    private readonly IComponentRegistry? _parentRegistry;
    
    internal IComponentRegistry? SendSignalsAs { get; set; }

    public ComponentRegistry() {
        
    }

    public ComponentRegistry(IComponentRegistry parentRegistry) {
        ArgumentNullException.ThrowIfNull(parentRegistry);
        
        _parentRegistry = parentRegistry;
    }
    
    public void Add<T>(T component) where T : notnull {
        if (_lockers is [var firstLocker, ..]) {
            firstLocker.Queue(() => Add(component));
            return;
        }
        Components.Add(component);

        if (component is ISignalEmitter emitter) {
            emitter.SignalTarget = SignalTarget.From(this);
        }

        if (component is ISignalListener<SelfAdded> listener) {
            listener.OnSignal(new SelfAdded(SendSignalsAs ?? this));
        }

        this.OnSignal(new ComponentAdded<T>(component));
    }

    public void Remove<T>(T component) where T : notnull {
        if (_lockers is [var firstLocker, ..]) {
            firstLocker.Queue(() => Remove(component));
            return;
        }
        
        Components.Remove(component);
        
        if (component is ISignalEmitter emitter) {
            emitter.SignalTarget = SignalTarget.Null;
        }
        
        if (component is ISignalListener<SelfRemoved> listener) {
            listener.OnSignal(new SelfRemoved());
        }

        if (component is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    public T? Get<T>() where T : class {
        return Components.OfType<T>().FirstOrDefault() ?? _parentRegistry?.Get<T>();
    }

    public IReadOnlyList<T> GetAll<T>() where T : class {
        if (_parentRegistry is { } parentRegistry) {
            var left = parentRegistry.GetAll<T>();
            var right = Components.OfType<T>();

            if (right.Count == 0)
                return left;

            if (left.Count == 0)
                return right;
            
            return new ConcatList<T>(left, right);
        }

        return Components.OfType<T>();
    }

    public IReadOnlyList<T> GetAll<T>(Type targetType) where T : class
    {
        if (_parentRegistry is { } parentRegistry) {
            var left = parentRegistry.GetAll<T>(targetType);
            var right = Components.OfType<T>(targetType);

            if (right.Count == 0)
                return left;

            if (left.Count == 0)
                return right;
            
            return new ConcatList<T>(left, right);
        }

        return Components.OfType<T>(targetType);
    }

    public IEnumerable<object> GetAll() {
        return Components;
    }

    private readonly List<Locker> _lockers = [];
    
    public IDisposable LockChanges() {
        var locker = new Locker();
        _lockers.Add(locker);
        locker.Queue(() => {
            var popped = _lockers[^1];
            if (popped != locker) {
                throw new OutOfOrderComponentRegistryUnlockException();
            }
            _lockers.RemoveAt(_lockers.Count - 1);
        });

        return locker;
    }

    class Locker : IDisposable {
        private List<Action>? _actions;

        public void Queue(Action onDispose) {
            _actions ??= [];
            _actions.Add(onDispose);
        }
        
        public void Dispose() {
            if (_actions is { } actions) {
                foreach (var action in actions) {
                    action();
                }
                _actions.Clear();
            }
        }
    }
}

public sealed class RequiredComponentMissingException(Type type) : Exception {
    public override string Message => $"Required component missing: {type.Name}";
}

public sealed class OutOfOrderComponentRegistryUnlockException : Exception {
    public override string Message => "ComponentRegistry's locks got disposed in different order than expected.";
}