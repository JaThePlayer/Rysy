using Rysy.Helpers;
using Rysy.Platforms;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Rysy;

public class Profile : IHasJsonCtx<Profile> {
    public static Profile Instance { get; internal set; }

    /// <summary>
    /// The celeste directory saved to the profile.
    /// For most purposes, <see cref="CelesteDirectory"/> should be used instead, as it takes commandline arguments into account.
    /// </summary>
    [JsonPropertyName("CelesteDirectory")]
    public string StoredCelesteDirectory { get; set; }

    [JsonIgnore]
    public string CelesteDirectory {
        get => RysyState.CmdArguments.CelesteDir ?? StoredCelesteDirectory;
        set {
            StoredCelesteDirectory = value;
            RysyState.CmdArguments.CelesteDir = null;
        }
    }

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

        if (profile.CelesteDirectory.IsNullOrWhitespace() && RysyState.CmdArguments.CelesteDir is {} celesteDir) {
            profile.CelesteDirectory = celesteDir;
        }

        return profile;
    }

    public static JsonTypeInfo<Profile> JsonCtx => DefaultJsonContext.Default.Profile;
}
