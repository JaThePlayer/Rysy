﻿using Rysy.Helpers;
using Rysy.Platforms;
using System.Text.Json;

namespace Rysy;

public sealed class Settings
{
    public static string SettingsFileLocation = $"{RysyPlatform.Current.GetSaveLocation()}/settings.json";

    public static Settings Instance { get; internal set; } = null!;

    public string CelesteDirectory { get; set; } = "";
    public string LastEditedMap { get; set; } = "";

    public string ModsDirectory => $"{CelesteDirectory}/Mods";

    public static Settings Load()
    {
        var settingsFile = SettingsFileLocation;
        string saveLocation = RysyPlatform.Current.GetSaveLocation();

        Settings? settings = null;

        if (!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation);
        }

        if (File.Exists(settingsFile))
        {
            try
            {
                using var stream = File.OpenRead(settingsFile);
                settings = JsonSerializer.Deserialize<Settings>(stream, JsonSerializerHelper.DefaultOptions);
            } catch (Exception e)
            {
                Logger.Write("Settings.Load", LogLevel.Error, $"Failed loading settings! {e}");
                throw;
            }
        }

        return settings ?? Save(new()
        {
            // there's no UI yet, so no way to change this in-game
            // there's also no automatic instal detection, so you'll have to edit it manually. Oh well
        });
    }

    public static Settings Save(Settings settings)
    {
        using var stream = File.OpenWrite(SettingsFileLocation);
        JsonSerializer.Serialize(stream, settings, typeof(Settings), JsonSerializerHelper.DefaultOptions);

        return settings;
    }
}
