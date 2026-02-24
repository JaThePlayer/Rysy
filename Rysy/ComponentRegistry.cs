using Rysy.Helpers;

namespace Rysy;

public interface IComponentRegistry
{
    void Add(object component);
    void Remove(object component);
    T? Get<T>() where T : class;
    IEnumerable<T> GetAll<T>() where T : class;
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

    public void Add(object component) {
        _newComponents.Add(component);
        parent.Add(component);
    }

    public void Remove(object component) {
        _newComponents.Remove(component);
        parent.Remove(component);
    }

    public T? Get<T>() where T : class {
        return parent.Get<T>();
    }

    public IEnumerable<T> GetAll<T>() where T : class {
        return parent.GetAll<T>();
    }
}

public sealed class ComponentRegistry : IComponentRegistry {
    private TypeTrackedList<object> Components { get; } = [];

    private readonly IComponentRegistry? _parentRegistry;

    public ComponentRegistry() {
        
    }

    public ComponentRegistry(IComponentRegistry parentRegistry) {
        ArgumentNullException.ThrowIfNull(parentRegistry);
        
        _parentRegistry = parentRegistry;
    }
    
    public void Add(object component) {
        Components.Add(component);
    }

    public void Remove(object component) {
        Components.Add(component);
    }

    public T? Get<T>() where T : class {
        return Components[typeof(T)].FirstOrDefault() as T ?? _parentRegistry?.Get<T>();
    }

    public IEnumerable<T> GetAll<T>() where T : class {
        if (_parentRegistry is { } parentRegistry) {
            return parentRegistry.GetAll<T>().Concat(Components[typeof(T)].Cast<T>());
        }
        return Components[typeof(T)].Cast<T>();
    }
}

public sealed class RequiredComponentMissingException(Type type) : Exception {
    public override string Message => $"Required component missing: {type.Name}";
}