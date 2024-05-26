using Rysy.Platforms;
using System.Text.Json.Serialization;

namespace Rysy;

public class Profile {
    public static Profile Instance { get; internal set; }

    public string CelesteDirectory { get; set; } = "";

    public string? ModDirectoryOverride { get; set; } = null;

    public Profile() {

    }

    public Profile(Profile from) {
        CelesteDirectory = from.CelesteDirectory;
        ModDirectoryOverride = from.ModDirectoryOverride;
    }

    public string ModsDirectory => ModDirectoryOverride ?? $"{CelesteDirectory}/Mods";

    public Profile Save() {
        SettingsHelper.Save(this, "profile.json", perProfile: true);

        return this;
    }

    public static Profile Load(bool setInstance = true) {
        if (Settings.Instance == null) {
            throw new Exception("Settings.Load() needs to be called before Profile.Load()");
        }

        var profile = RysyPlatform.Current.ForcedProfile()?.Profile ?? SettingsHelper.Load<Profile>("profile.json", perProfile: true);

        if (setInstance) {
            Instance = profile;
        }

        return profile;
    }
}
