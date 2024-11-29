﻿//#define DOT_TRACE

using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rysy;


public static class SettingsHelper {
    public static bool ReadSettings { get; set; } = true;

    public static IWriteableModFilesystem GetFilesystem(bool perProfile) {
        return RysyPlatform.Current.GetRysyAppDataFilesystem(
            perProfile ? RysyPlatform.Current.ForcedProfile()?.Name ?? Settings.Instance.CurrentProfile : null);
    }

    public static T Load<T>(string filename, bool perProfile = false) where T : class, new() {
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

    public static T Save<T>(T settings, string filename, bool perProfile = false) where T : class, new() {
        if (!ReadSettings)
            return new();

        var fs = GetFilesystem(perProfile);
        fs.TryWriteToFile(filename, stream => {
            JsonSerializer.Serialize(stream, settings, JsonSerializerHelper.SettingsOptions);
        });
        
        return settings;
    }
}

public sealed class Settings {
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

    private static Settings _Instance = null!;
    public static Settings Instance {
        get => _Instance;
        internal set {
            if (_Instance != value) {
                _Instance = value;
                OnLoaded?.Invoke(value);
            }
        }
    }

    public static void ChangeProfile(string name, bool isNew) {
        RysyState.CmdArguments.Profile = null;
        
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


    private string _theme = "dark";
    public string Theme {
        get => _theme;
        set {
            _theme = value;
            if (RysyState.ImGuiAvailable && UiEnabled)
                LoadTheme(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LoadTheme(string val) {
        ImGuiThemer.LoadTheme(val);
    }

    private int _fontSize = 16;
    public int FontSize {
        get => _fontSize;
        set {
            _fontSize = value;
            if (RysyState.ImGuiAvailable && UiEnabled)
                RysyState.OnEndOfThisFrame += () => ImGuiThemer.SetFontSize(value);
        }
    }
    
    private float _triggerFontScale = 0.5f;
    public float TriggerFontScale {
        get => _triggerFontScale;
        set {
            _triggerFontScale = value;
            if (EditorState.Map is { } map) {
                map.ClearRenderCache();
            }
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

    private bool _SmartFramerate = false;
    public bool SmartFramerate {
        get => _SmartFramerate;
        set {
            _SmartFramerate = value;
            SmartFPSHandler.OnToggle();
        }
    }

    public bool MinifyClipboard { get; set; } = false;


    private bool _BorderlessFullscreen = false;
    // has issues with mouse positions
    public bool BorderlessFullscreen {
        get => _BorderlessFullscreen;
        set {
            _BorderlessFullscreen = value;

            RysyEngine.ToggleBorderlessFullscreen(value);
        }
    }

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

    #endregion
}
