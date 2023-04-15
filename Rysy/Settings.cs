#define DOT_TRACE

using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Platforms;
using Rysy.Scenes;
using System.Text.Json;

namespace Rysy;


public static class SettingsHelper {
    public static bool ReadSettings = true;

    public static string GetFullPath(string settingFileName, bool perProfile) => perProfile && Settings.Instance is { }
    ? $"{RysyPlatform.Current.GetSaveLocation()}/Profiles/{Settings.Instance.Profile}/{settingFileName}"
    : $"{RysyPlatform.Current.GetSaveLocation()}/{settingFileName}";

    //public static bool SaveFileExists(string filename) => File.Exists(GetFullPath(filename));

    public static T Load<T>(string filename, bool perProfile = false) where T : class, new() {
        if (!ReadSettings)
            return new();
        
        var settingsFile = GetFullPath(filename, perProfile);
        var saveLocation = Path.GetDirectoryName(settingsFile);

        T? settings = null;

        if (!Directory.Exists(saveLocation)) {
            Directory.CreateDirectory(saveLocation!);
        }

        if (File.Exists(settingsFile)) {
#if DOT_TRACE
            int attempts = 0;
        tryRead:
#endif
            try {
                using var stream = File.OpenRead(settingsFile);
                settings = JsonSerializer.Deserialize<T>(stream, JsonSerializerHelper.SettingsOptions);
            } catch (Exception e) {
                Logger.Write("Settings.Load", LogLevel.Error, $"Failed loading {typeof(T)}! {e}");
#if DOT_TRACE
                // dot trace's precise profiler breaks file reading while loading??????
                attempts++;
                if (attempts < 100) {
                    Thread.Sleep(10);
                    goto tryRead;
                }
#endif
                throw;
            }
        }

        return settings ?? Save<T>(new() {
            // there's no UI yet, so no way to change this in-game
            // there's also no automatic instal detection, so you'll have to edit it manually. Oh well
        }, filename, perProfile);
    }

    public static T Save<T>(T settings, string filename, bool perProfile = false) where T : class, new() {
        if (!ReadSettings)
            return new();

        var settingsFile = GetFullPath(filename, perProfile);
        var saveLocation = Path.GetDirectoryName(settingsFile);

        if (!Directory.Exists(saveLocation)) {
            Directory.CreateDirectory(saveLocation!);
        }

        using var stream = File.Exists(settingsFile)
            ? File.Open(settingsFile, FileMode.Truncate)
            : File.Open(settingsFile, FileMode.CreateNew);
        JsonSerializer.Serialize(stream, settings, typeof(T), JsonSerializerHelper.SettingsOptions);

        return settings;
    }
}

public sealed class Settings {
    public static string SettingsFileLocation = $"settings.json";

    public static Settings Load(bool setInstance = true) {
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

    public static Settings Instance { get; internal set; } = null!;

    #region Serialized
    public string Profile { get; set; } = "Default";

    public bool LogMissingEntities { get; set; } = false;
    public bool LogTextureLoadTimes { get; set; } = false;

    private string _theme = "dark";
    public string Theme {
        get => _theme;
        set {
            _theme = value;
            if (RysyEngine.ImGuiAvailable)
                ImGuiThemer.LoadTheme(value);
        }
    }

    private int _fontSize = 16;
    public int FontSize {
        get => _fontSize;
        set {
            _fontSize = value;
            if (RysyEngine.ImGuiAvailable)
                RysyEngine.OnFrameEnd += () => ImGuiThemer.SetFontSize(value);
        }
    }

    public Dictionary<string, string> Hotkeys { get; set; } = new();

    public int MaxBackups { get; set; } = 25;

    private float _HiddenLayerAlpha = 0.3f;
    public float HiddenLayerAlpha {
        get => _HiddenLayerAlpha;
        set {
            _HiddenLayerAlpha = value;
            if (RysyEngine.Scene is EditorScene editor) {
                editor.ClearMapRenderCache();
            }
        }
    }

    private int _TargetFps = 60;
    public int TargetFps {
        get => _TargetFps;
        set {
            _TargetFps = value;
            RysyEngine.SetTargetFps(value);
        }
    }

    private bool _VSync = true;
    public bool VSync {
        get => _VSync;
        set {
            _VSync = value;
            RysyEngine.ToggleVSync(value);
        }
    }

    private bool _SmartFramerate = true;
    public bool SmartFramerate {
        get => _SmartFramerate;
        set {
            _SmartFramerate = value;
            SmartFPSHandler.OnToggle();
        }
    }

    public bool MinifyClipboard { get; set; } = true;

    public bool LogMissingTextures { get; set; } = true;


    private bool _BorderlessFullscreen = false;
    // has issues with mouse positions
    public bool BorderlessFullscreen {
        get => _BorderlessFullscreen;
        set {
            _BorderlessFullscreen = value;

            RysyEngine.ToggleBorderlessFullscreen(value);
        }
    }

#warning Remove
    public string? LonnPluginPath { get; set; }
    #endregion
}
