using System.Collections.Concurrent;

namespace Rysy.Helpers;

public static class DisposableDictionaryExt {
    extension<TKey>(ConcurrentDictionary<TKey, IDisposable> dictionary) where TKey : notnull {
        /// <summary>
        /// Sets dictionary[key] = value, while also disposing the disposable that is currently assigned to key.
        /// If value is null, the key is instead removed from the dictionary and the old disposable is also disposed.
        /// </summary>
        public void SetAndDisposeOld(TKey key, IDisposable? value) {
            if (value is null) {
                if (dictionary.Remove(key, out var oldDisposable)) {
                    oldDisposable.Dispose();
                }

                return;
            }
            
            dictionary.AddOrUpdate(key, value, (_, oldDisposable) => {
                oldDisposable.Dispose();
                return value;
            });
        }

        public void DisposeAllAndClear() {
            foreach (var disposable in dictionary.Values) {
                disposable.Dispose();
            }
            dictionary.Clear();
        }
    }
}