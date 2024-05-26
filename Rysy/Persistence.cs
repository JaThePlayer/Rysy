using Rysy.Helpers;
using System.Text.Json;

namespace Rysy;

public class Persistence {
    static Persistence() {
        BackgroundTaskHelper.RegisterOnInterval(TimeSpan.FromSeconds(5), () => {
            if (Instance is not { } || !RecentlyEdited)
                return;

            RecentlyEdited = false;

            try {
                Save(Instance);
            } catch {
                // oh well, maybe we can save in 5 seconds
            }
        });
    }

    #region Helpers

    public static Persistence Instance { get; set; } = null!;

    private static string FileLocation { get; set; } = $"persistence.json";


    private static bool RecentlyEdited { get; set; } = false;

    public static Persistence Load() {
        return SettingsHelper.Load<Persistence>(FileLocation, perProfile: true);
    }

    public static Persistence Save(Persistence settings) {
        return SettingsHelper.Save<Persistence>(settings, FileLocation, perProfile: true);
    }

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

    public void Set<T>(string key, T value) {
        Values[key] = value!;

        RecentlyEdited = true;
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

    private bool _FGTilesVisible = true;
    public bool FGTilesVisible {
        get => _FGTilesVisible;
        set {
            if (_FGTilesVisible != value) {
                _FGTilesVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearFgTilesRenderCache());

                RecentlyEdited = true;
            }
        }
    }

    private bool _BGTilesVisible = true;
    public bool BGTilesVisible {
        get => _BGTilesVisible;
        set {
            if (_BGTilesVisible != value) {
                _BGTilesVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearBgTilesRenderCache());
                RecentlyEdited = true;
            }
        }
    }

    private bool _FGDecalsVisible = true;
    public bool FGDecalsVisible {
        get => _FGDecalsVisible;
        set {
            if (_FGDecalsVisible != value) {
                _FGDecalsVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearFgDecalsRenderCache());

                RecentlyEdited = true;
            }
        }
    }

    private bool _BGDecalsVisible = true;
    public bool BGDecalsVisible {
        get => _BGDecalsVisible;
        set {
            if (_BGDecalsVisible != value) {
                _BGDecalsVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearBgDecalsRenderCache());

                RecentlyEdited = true;
            }
        }
    }

    private bool _EntitiesVisible = true;
    public bool EntitiesVisible {
        get => _EntitiesVisible;
        set {
            if (_EntitiesVisible != value) {
                _EntitiesVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearEntityRenderCache());

                RecentlyEdited = true;
            }
        }
    }

    private bool _TriggersVisible = true;
    public bool TriggersVisible {
        get => _TriggersVisible;
        set {
            if (_TriggersVisible != value) {
                _TriggersVisible = value;

                EditorState.Map?.Rooms.ForEach(r => r.ClearTriggerRenderCache());

                RecentlyEdited = true;
            }
        }
    }

    public bool HistoryWindowOpen = false;
    #endregion

    public struct RecentMap {
        public string Filename { get; set; }
        public string Name { get; set; }
    }
}
