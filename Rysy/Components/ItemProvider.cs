using Rysy.Helpers;
using Rysy.Signals;

namespace Rysy.Components;

/// <summary>
/// A component that provides a cacheable list of <typeparamref name="T"/> elements.
/// For use with the <see cref="IItemProviderExtensions.GetAllIncludingProvidersCache"/> method.
/// </summary>
public interface IItemProvider<T> {
    public Cache<IReadOnlyList<T>> ElementCache { get; }
}

public static class IItemProviderExtensions {
    extension(IComponentRegistry registry) {
        /// <summary>
        /// Creates a <see cref="Cache{IReadOnlyList}"/>, which will return all components of type <see cref="T"/> or components provided by <see cref="IItemProvider{T}"/>s in the registry.
        /// The cache will be invalidated when new providers are added or existing ones invalidate their cache.
        /// </summary>
        public Cache<IReadOnlyList<T>> GetAllIncludingProvidersCache<T>() where T : class {
            var listener = registry.AddIfMissing<CreateAllFromItemProvidersCacheListener<T>>();

            return listener.Cache;
        }
    }

    private class CreateAllFromItemProvidersCacheListener<T>
        : ISignalListener<ComponentAdded>, ISignalListener<ComponentRemoved>, IHasComponentRegistry
        where T : class {
        public Cache<IReadOnlyList<T>> Cache { get; }
        
        public IComponentRegistry? Registry { get; set; }

        public CreateAllFromItemProvidersCacheListener() {
            Cache = new Cache<IReadOnlyList<T>>(new CacheToken(), Generator);
        }
        
        private IReadOnlyList<T> Generator() {
            if (Registry is null)
                return [];
            
            List<T> ret = [];

            foreach (var provider in Registry.EnumerateAllLocked<IItemProvider<T>>()) {
                ret.AddRange(provider.ElementCache.Value);
            }
            foreach (var item in Registry.EnumerateAllLocked<T>()) {
                ret.AddRange(item);
            }
            
            return ret;
        }

        public void OnSignal(ComponentAdded signal) {
            if (signal.Component is IItemProvider<T> component) {
                component.ElementCache.Token.OnInvalidate += Cache.Token.InvalidateThenReset;
                Cache.Token.InvalidateThenReset();
            }

            if (signal.Component is T t) {
                Cache.Token.InvalidateThenReset();
            }
        }

        public void OnSignal(ComponentRemoved signal) {
            if (signal.Component is IItemProvider<T> component) {
                component.ElementCache.Token.OnInvalidate -= Cache.Token.InvalidateThenReset;
                Cache.Token.InvalidateThenReset();
            }

            if (signal.Component is T t) {
                Cache.Token.InvalidateThenReset();
            }
        }
    }
}
