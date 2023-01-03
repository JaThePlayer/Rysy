using Rysy.Helpers;
using Rysy.Platforms;
using System.Text.Json;

namespace Rysy;

public static class SettingsHelper
{
    public static string GetFullPath(string settingFileName) => Profile.CurrentProfile is { } 
    ? $"{RysyPlatform.Current.GetSaveLocation()}/{Profile.CurrentProfile.Name}/{settingFileName}"
    : $"{RysyPlatform.Current.GetSaveLocation()}/{settingFileName}";

    public static bool SaveFileExists(string filename) => File.Exists(GetFullPath(filename));

    public static T Load<T>(string filename) where T : class, new() {
        var settingsFile = GetFullPath(filename);
        var saveLocation = Path.GetDirectoryName(settingsFile);

        T? settings = null;

        if (!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation!);
        }

        if (File.Exists(settingsFile))
        {
            try
            {
                using var stream = File.OpenRead(settingsFile);
                settings = JsonSerializer.Deserialize<T>(stream, JsonSerializerHelper.DefaultOptions);
            }
            catch (Exception e)
            {
                Logger.Write("Settings.Load", LogLevel.Error, $"Failed loading settings! {e}");
                throw;
            }
        }

        return settings ?? Save<T>(new()
        {
            // there's no UI yet, so no way to change this in-game
            // there's also no automatic instal detection, so you'll have to edit it manually. Oh well
        }, filename);
    }

    public static T Save<T>(T settings, string filename) {
        var settingsFile = GetFullPath(filename);

        
        using var stream = File.Exists(settingsFile) 
            ? File.Open(settingsFile, FileMode.Truncate)
            : File.Open(settingsFile, FileMode.CreateNew);
        JsonSerializer.Serialize(stream, settings, typeof(T), JsonSerializerHelper.DefaultOptions);

        return settings;
    }
}

public sealed class Settings
{
    public static string SettingsFileLocation = $"settings.json";

    public static Settings Load()
    {
        return SettingsHelper.Load<Settings>(SettingsFileLocation);
    }

    public static Settings Save(Settings settings)
    {
        return SettingsHelper.Save<Settings>(settings, SettingsFileLocation);
    }

    public sealed class HotkeySettings {
        
    }

    public static Settings Instance { get; internal set; } = null!;

    #region Serialized
    public string CelesteDirectory { get; set; } = "";
    public string LastEditedMap { get; set; } = "";

    public bool LogMissingEntities { get; set; } = false;
    public bool LogTextureLoadTimes { get; set; } = false;

    public HotkeySettings Keybinds { get; set; } = new();
    #endregion

    public string ModsDirectory => $"{CelesteDirectory}/Mods";
}
