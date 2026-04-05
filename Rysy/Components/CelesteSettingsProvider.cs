using Rysy.Helpers;
using Rysy.Mods;
using YamlDotNet.Serialization;

namespace Rysy.Components;

/// <summary>
/// Provides a way to read in-game settings from Rysy.
/// </summary>
public interface ICelesteSettingsProvider {
    /// <summary>
    /// Reads the in-game settings for the given mod.
    /// Returns null on failure or if the settings file does not exist.
    /// </summary>
    T? ReadModSettings<T>(string modName);
    
    /// <summary>
    /// Reads Everest's settings file, returning default values if the file is not present.
    /// </summary>
    EverestInGameSettings ReadEverestSettings() {
        if (ReadModSettings<EverestInGameSettings>("everest") is { } readModSettings)
            return readModSettings;
            
        return new EverestInGameSettings();
    }
}

public sealed class CelesteSettingsProvider(IModFilesystem celesteDirFs, IRysyLogger<CelesteSettingsProvider> logger) : ICelesteSettingsProvider {
    public T? ReadModSettings<T>(string modName) {
        var text = celesteDirFs.TryReadAllText($"Saves/modsettings-{modName}.celeste");
        if (text is null) {
            return default;
        }

        try {
            return YamlHelper.Deserializer.Deserialize<T>(text);
        } catch (Exception ex) {
            logger.Error(ex, $"Failed to deserialize in-game settings for '{modName}' to '{typeof(T)}'");
        }

        return default;
    }
}

public sealed class EverestInGameSettings {
    // Everest stores this as an int, but ports are 16-bit...
    [YamlMember(Alias = "DebugRCPort")]
    public ushort DebugRcPort { get; set; } = 32270;
}
