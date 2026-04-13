using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

public static class AttachHelper {
    /*
    public static void ClearAll<TObj, TAttached>() where TObj : class where TAttached : class, IAttachable {
        Storage<TObj, TAttached>.Stored.Clear();
    }
    
    extension<TObj>(TObj obj) where TObj : class {
        public bool TryGetAttached<T>([NotNullWhen(true)] out T? attached) where T : class, IAttachable {
            return Storage<TObj, T>.Stored.TryGetValue(obj, out attached);
        }
        
        public T GetOrCreateAttached<T>(Func<TObj, T> factory) where T : class, IAttachable {
            return Storage<TObj, T>.Stored.GetOrAdd(obj, factory);
        }
        
        public T GetOrCreateAttached<T, TState>(Func<TObj, TState, T> factory, TState state) where T : class, IAttachable {
            return Storage<TObj, T>.Stored.GetOrAdd(obj, factory, state);
        }
    }
*/

}

internal sealed class AttachedStorage<TObj, TAttached> where TObj : class where TAttached : class, IAttachable {
    public ConditionalWeakTable<TObj, TAttached> Stored { get; } = [];
    
    public bool TryGetAttached(TObj obj, [NotNullWhen(true)] out TAttached? attached) {
        return Stored.TryGetValue(obj, out attached);
    }
        
    public TAttached GetOrCreateAttached(TObj obj, Func<TObj, TAttached> factory) {
        return Stored.GetOrAdd(obj, factory);
    }
        
    public TAttached GetOrCreateAttached<TState>(TObj obj, Func<TObj, TState, TAttached> factory, TState state) {
        return Stored.GetOrAdd(obj, factory, state);
    }
}

internal interface IAttachable;
