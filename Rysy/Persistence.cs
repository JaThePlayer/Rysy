using Rysy.Helpers;
using Rysy.Signals;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Rysy;

public class Persistence : ISignalEmitter, IHasJsonCtx<Persistence> {
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
        ref var prev = ref CollectionsMarshal.GetValueRefOrAddDefault(Values, key, out var exists);
        
        Change(key, ref prev, value);

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

    private void Change<T>(string name, ref T field, T newValue) {
        if (field?.Equals(newValue) ?? newValue == null)
            return;
        var old = field;
        field = newValue;
        this.Emit(new PersistenceChanged(this, name));
        this.Emit(new PersistenceChanged<T>(this, name, old, newValue));

        Save(this);
    }
    
    private void Change<T>(string name, ref object? field, T newValue) {
        if (field?.Equals(newValue) ?? newValue == null)
            return;
        var old = field is T t ? t : default;
        field = newValue;
        this.Emit(new PersistenceChanged(this, name));
        this.Emit(new PersistenceChanged<T>(this, name, old, newValue));

        Save(this);
    }
    
    #region Serialized
    public List<RecentMap> RecentMaps { get; set; } = new();
    public Dictionary<string, object> Values { get; set; } = new();

    public bool FgTilesVisible {
        get;
        set => Change(nameof(FgTilesVisible), ref field, value);
    } = true;

    public bool BgTilesVisible {
        get;
        set => Change(nameof(BgTilesVisible), ref field, value);
    } = true;

    public bool FgDecalsVisible {
        get;
        set => Change(nameof(FgDecalsVisible), ref field, value);
    } = true;

    public bool BgDecalsVisible {
        get;
        set => Change(nameof(BgDecalsVisible), ref field, value);
    } = true;

    public bool EntitiesVisible {
        get;
        set => Change(nameof(EntitiesVisible), ref field, value);
    } = true;

    public bool TriggersVisible {
        get;
        set => Change(nameof(TriggersVisible), ref field, value);
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
    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}
