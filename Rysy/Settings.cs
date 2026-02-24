//#define DOT_TRACE

using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using Rysy.Signals;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Rysy;


public static class SettingsHelper {
    public static bool ReadSettings { get; set; } = true;

    public static IWriteableModFilesystem GetFilesystem(bool perProfile) {
        return RysyPlatform.Current.GetRysyAppDataFilesystem(
            perProfile ? RysyPlatform.Current.ForcedProfile()?.Name ?? Settings.Instance.CurrentProfile : null);
    }

    public static T Load<T>(string filename, bool perProfile = false) where T : class, IHasJsonCtx<T>, new() {
        if (!ReadSettings)
            return new();

        var fs = GetFilesystem(perProfile);

        if (fs.OpenFile(filename, stream => {
                try {
                    return JsonSerializer.Deserialize<T>(stream, JsonSerializerHelper.SettingsOptions);
                } catch (Exception e) {
                    Logger.Write("Settings.Load", LogLevel.Error, $"Failed loading {typeof(T)}! {e}");
                    return null;
                }
            }) is {} settings) {
            return settings;
        }
        
        Logger.Write("Settings.Load", LogLevel.Info, $"Creating and saving new {typeof(T).Name} at {filename}{(perProfile ? " (per-profile)" : "")}");
        return Save<T>(new() { }, filename, perProfile);
    }

    public static T Save<T>(T settings, string filename, bool perProfile = false) where T : class, IHasJsonCtx<T>, new() {
        if (!ReadSettings)
            return new();

        var fs = GetFilesystem(perProfile);
        fs.TryWriteToFile(filename, stream => {
            JsonSerializer.Serialize(stream, settings, JsonSerializerHelper.SettingsOptions);
        });
        
        return settings;
    }
}

public sealed partial class Settings : IHasJsonCtx<Settings>, ISignalEmitter {
    public static bool UiEnabled { get; set; }
    
    public static string SettingsFileLocation { get; } = $"settings.json";

    public static Settings Load(bool setInstance = true, bool uiEnabled = true) {
        UiEnabled = uiEnabled;
        
        var settings = SettingsHelper.Load<Settings>(SettingsFileLocation);

        if (setInstance)
            Instance = settings;

        return settings;
    }

    public static Settings Save(Settings settings) {
        return SettingsHelper.Save<Settings>(settings, SettingsFileLocation);
    }

    public Settings Save() {
        return Save(Instance);
    }

    public string GetHotkey(string name) {
        return Hotkeys.GetValueOrDefault(name, "");
    }

    public string GetOrCreateHotkey(string name, string? defaultHotkey = null) {
        if (!Hotkeys.TryGetValue(name, out var hotkey)) {
            if (defaultHotkey is { }) {
                Hotkeys[name] = defaultHotkey;
                Save();
            }

            return defaultHotkey ?? "";
        }

        return hotkey;
    }

    public static event Action<Settings> OnLoaded;

    public static Settings Instance {
        get;
        internal set {
            if (field != value) {
                field = value;
                OnLoaded?.Invoke(value);
            }
        }
    }

    public static void ChangeProfile(string name, bool isNew) {
        RysyState.CmdArguments.Profile = null;
        RysyState.CmdArguments.CelesteDir = null;
        
        Instance.Profile = name;
        Save(Instance);
        
        if (isNew) {
            var profile = new Profile();
            profile.Save();
            Rysy.Profile.Instance = profile;
            Persistence.Instance = new();
            Persistence.Save(Persistence.Instance);
        }

        RysyEngine.QueueReload();
    }

    /// <summary>
    /// Current profile, taking into accound command line arguments before the actual value stored in the settings file.
    /// </summary>
    public string CurrentProfile => RysyState.CmdArguments.Profile ?? Profile;
    
    #region Serialized
    public string Profile { get; set; } = "Default";

    public bool LogMissingEntities { get; set; } = false;
    public bool LogTextureLoadTimes { get; set; } = false;

    public bool LogSpriteCachingTimes { get; set; } = false;

    public bool LogMissingTextures { get; set; } = false;

    public bool LogPreloadingTextures { get; set; } = false;
    
    public bool LogMissingFieldTypes { get; set; }


    public string Theme {
        get;
        set {
            var old = field;
            field = value;
            this.Emit(new SettingsChanged<string>(this, nameof(Theme), old, value));
        }
    } = "dark";

    public string Font {
        get;
        set {
            var old = field;
            field = value;
            this.Emit(new SettingsChanged<string>(this, nameof(Font), old, value));
        }
    } = "RobotoMono";

    public bool UseBoldFontByDefault {
        get;
        set {
            var old = field;
            field = value;
            this.Emit(new SettingsChanged<bool>(this, nameof(UseBoldFontByDefault), old, value));
        }
    } = true;

    public int FontSize {
        get;
        set {
            var old = field;
            field = value;
            this.Emit(new SettingsChanged<int>(this, nameof(FontSize), old, value));
        }
    } = 16;

    public float TriggerFontScale {
        get;
        set {
            field = value;
            EditorState.Current?.Map?.ClearRenderCache();
        }
    } = 0.5f;

    public Dictionary<string, string> Hotkeys { get; set; } = new();

    public int MaxBackups { get; set; } = 25;

    public float HiddenLayerAlpha {
        get;
        set {
            field = value;
            EditorState.Current?.Map?.ClearRenderCache();
        }
    } = 0.3f;

    public int TargetFps {
        get;
        set {
            field = value;
            RysyEngine.SetTargetFps(value);
        }
    } = 60;

    public bool VSync {
        get;
        set {
            field = value;
            RysyEngine.ToggleVSync(value);
        }
    } = true;

    public bool SmartFramerate {
        get;
        set {
            field = value;
            SmartFpsHandler.OnToggle();
        }
    } = false;

    public bool MinifyClipboard { get; set; } = false;


    // has issues with mouse positions
    public bool BorderlessFullscreen {
        get;
        set {
            field = value;

            RysyEngine.ToggleBorderlessFullscreen(value);
        }
    } = false;

    public bool ReadBlacklist { get; set; } = true;

    public int? StartingWindowWidth { get; set; } = null;
    public int? StartingWindowHeight { get; set; } = null;
    public int? StartingWindowX { get; set; } = null;
    public int? StartingWindowY { get; set; } = null;

    public string? FontFile { get; set; } = null;

    public bool ShowPlacementIcons { get; set; } = true;

    public bool StylegroundPreview { get; set; } = true;

    public bool AnimateStylegrounds { get; set; } = true;

    public bool RenderFgStylegroundsInFront { get; set; } = false;

    public bool OnlyRenderStylesAtRealScale { get; set; } = false;

    public bool ClearRenderCacheForOffScreenRooms { get; set; } = true;

    public bool Animate { get; set; } = true;

    public bool TrimEntities { get; set; } = false;

    public bool MouseWrapping { get; set; } = false;

    public float TouchpadPanSpeed { get; set; } = 100f;

    public LogLevel MinimumNotificationLevel { get; set; } = LogLevel.Warning;

    public bool NotificationWindowOpen { get; set; } = false;

    public int GridSize { get; set; } = 8;

    /// <summary>
    /// Whether Texture2D instances can be created outside the main thread.
    /// Not officially supported by FNA, but it works?
    /// Leaving it here so others can experiment, but from my testing it makes no difference on D3D11,
    /// and heavily regresses OpenGL, making this not worth it.
    /// (Used to be the default on older versions of Rysy, disabled now)
    /// </summary>
    public bool AllowMultithreadedTextureCreation { get; set; }

    public float PlaytestTrailOpacity { get; set; } = 0.45f;
    #endregion

    public static JsonTypeInfo<Settings> JsonCtx => DefaultJsonContext.Default.Settings;

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}
