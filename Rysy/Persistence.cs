using Rysy.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Rysy;

public class Persistence : IHasJsonCtx<Persistence> {
    #region Helpers

    public static Persistence Instance { get; set; } = null!;

    private static string FileLocation { get; set; } = $"persistence.json";

    public static Persistence Load() {
        return SettingsHelper.Load<Persistence>(FileLocation, perProfile: true);
    }

    public static Persistence Save(Persistence settings) {
        return SettingsHelper.Save<Persistence>(settings, FileLocation, perProfile: true);
    }
    
#if NET9_0_OR_GREATER
    public T Get<T>(ReadOnlySpan<char> key, T defaultValue) {
        var values = Values.GetAlternateLookup<ReadOnlySpan<char>>();
        if (values.TryGetValue(key, out var ret)) {
            if (ret is JsonElement e) {
                ret = e.Deserialize<T>();
                values[key] = ret!;
            }
            return (T) ret!;
        }
        Set(key.ToString(), defaultValue);

        return defaultValue;
    }
    
    public T Get<T>(string key, T defaultValue) {
        return Get(key.AsSpan(), defaultValue); 
    }
    
    public T Get<T>(Interpolator.Handler key, T defaultValue) {
        return Get(key.Result, defaultValue);
    }
#else    
    public T Get<T>(string key, T defaultValue) {
        if (Values.TryGetValue(key, out var ret)) {
            if (ret is JsonElement e) {
                ret = e.Deserialize<T>();
                Values[key] = ret!;
            }
            return (T) ret!;
        }
        Set(key, defaultValue);

        return defaultValue;
    }
#endif

    public void Set<T>(string key, T value) {
        Values[key] = value!;

        Save(this);
    }

    public void PushRecentMap(Map map) {
        if (map.Filepath is null)
            return;
        var entry = new RecentMap() {
            Name = Path.GetRelativePath(Profile.Instance.ModsDirectory, map.Filepath),
            Filename = map.Filepath,
        };
        // remove duplicate entries
        RecentMaps.Remove(entry);
        RecentMaps.Insert(0, entry);

        if (RecentMaps.Count > 10) {
            RecentMaps.RemoveAt(10);
        }

        Save(this);
    }
    #endregion

    public string? LastEditedMap => RecentMaps.FirstOrDefault(default(RecentMap)).Filename;

    #region Serialized
    public List<RecentMap> RecentMaps { get; set; } = new();
    public Dictionary<string, object> Values { get; set; } = new();

    public bool FgTilesVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearFgTilesRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool BgTilesVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearBgTilesRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool FgDecalsVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearFgDecalsRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool BgDecalsVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearBgDecalsRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool EntitiesVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearEntityRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool TriggersVisible {
        get;
        set {
            if (field != value) {
                field = value;

                EditorState.Current?.Map?.Rooms.ForEach(r => r.ClearTriggerRenderCache());
                Save(this);
            }
        }
    } = true;

    public bool HistoryWindowOpen = false;

    public const string ColorgradePreviewMapDefaultValue = "mapDefault";

    public string ColorgradePreview { get; set; } = ColorgradePreviewMapDefaultValue;
    #endregion

    public struct RecentMap {
        public string Filename { get; set; }
        public string Name { get; set; }
    }

    public static JsonTypeInfo<Persistence> JsonCtx => DefaultJsonContext.Default.Persistence;
}
