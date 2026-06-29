using System.Diagnostics;

namespace Rysy.Helpers;

/// <summary>
/// Attaches a timestamp onto an object. Each time the object is accessed, the timestamp is updated.
/// </summary>
public sealed class Timestamped<T>(T value) {
    /// <summary>
    /// The inner value. Accessing this property updates <see cref="LastAccess"/>
    /// </summary>
    public T Value {
        get {
            LastAccess = Stopwatch.GetTimestamp();
            return field;
        }
    } = value;
    
    /// <summary>
    /// The timestamp, obtained via <see cref="Stopwatch.GetTimestamp()"/>, of the last access to the <see cref="Value"/> property.
    /// </summary>
    public long LastAccess { get; private set; } = Stopwatch.GetTimestamp();

    public static implicit operator Timestamped<T>(T value) => new(value);
    
    public static implicit operator T(Timestamped<T> value) => value.Value;
}

public static class TimestampedExt {
    extension<TKey, TValue>(IDictionary<TKey, Timestamped<TValue>> dictionary) {
        public IEnumerable<KeyValuePair<TKey, Timestamped<TValue>>> EnumerateNotAccessedSince(long timestamp) {
            return dictionary.Where(kvp => kvp.Value.LastAccess < timestamp);
        }
        
        public void RemoveNotAccessedSince(long timestamp) {
            foreach (var key in dictionary.EnumerateNotAccessedSince(timestamp).ToList()) {
                dictionary.Remove(key);
            }
        }
    }
}
