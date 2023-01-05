using System.Text.Json;

namespace Rysy;

public class Persistence {
    #region Helpers
    public static Persistence Instance { get; internal set; } = null!;

    public static string SettingsFileLocation = $"persistence.json";

    public static Persistence Load() {
        return SettingsHelper.Load<Persistence>(SettingsFileLocation);
    }

    public static Persistence Save(Persistence settings) {
        return SettingsHelper.Save<Persistence>(settings, SettingsFileLocation);
    }

    public T Get<T>(string key, T defaultValue) {
        if (Values.TryGetValue(key, out var ret)) {
            if (ret is JsonElement e) {
                ret = e.Deserialize<T>();
                Values[key] = ret!;
            }
            return (T) ret;
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value) {
        Values[key] = value!;

#warning TODO: Don't save immediately, only once in a while
        Save(this);
    }
    #endregion

    #region Serialized
    public Dictionary<string, object> Values { get; set; } = new();
    #endregion
}
