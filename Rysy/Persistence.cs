using System.Text.Json;

namespace Rysy;

public class Persistence {
    #region Helpers
    public static Persistence Instance { get; internal set; } = null!;

    public static string SettingsFileLocation = $"persistence.json";

    public static Persistence Load() {
        return SettingsHelper.Load<Persistence>(SettingsFileLocation, perProfile: true);
    }

    public static Persistence Save(Persistence settings) {
        return SettingsHelper.Save<Persistence>(settings, SettingsFileLocation, perProfile: true);
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

    public void PushRecentMap(Map map) {
        if (map.Filename is null)
            return;
        var entry = new RecentMap() {
            Package = map.Package ?? map.Filename,
            Filename = map.Filename,
        };
        RecentMaps.Remove(entry);
        RecentMaps.Insert(0, entry);

#warning TODO: Don't save immediately, only once in a while
        Save(this);
    }
    #endregion

    public string? LastEditedMap => RecentMaps.FirstOrDefault(default(RecentMap)).Filename;

    #region Serialized
    public List<RecentMap> RecentMaps { get; set; } = new();
    public Dictionary<string, object> Values { get; set; } = new();
    #endregion

    public struct RecentMap {
        public string Package { get; set; }
        public string Filename { get; set; }
    }
}
