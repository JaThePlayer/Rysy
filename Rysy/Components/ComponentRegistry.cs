using Rysy.Components;
using Rysy.Helpers;
using Rysy.Signals;
using System.Diagnostics.CodeAnalysis;

namespace Rysy;

public interface IComponentRegistry : ISignalListener
{
    void Add<T>(T component) where T : notnull;
    void Remove<T>(T component) where T : notnull;
    T? Get<T>() where T : class;
    IReadOnlyList<T> GetAll<T>() where T : class;

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
}

public sealed class RequiredComponentMissingException(Type type) : Exception {
    public override string Message => $"Required component missing: {type.Name}";
}