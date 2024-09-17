using Rysy.Helpers;

namespace Rysy;

public record class RoomAttributes {
    public RoomAttributes Copy() => new(this);

    public string Name { get; set; } = "";
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public bool DelayAltMusicFade;

    public float CameraOffsetX;
    public float CameraOffsetY;

    /// <summary>
    /// Debug color
    /// </summary>
    public int C;

    public CelesteEnums.WindPatterns WindPattern;
    public bool Space;
    public string AmbienceProgress = "";
    public bool DisableDownTransition;
    public bool Dark;
    public bool Whisper;
    public bool Underwater;

    public string Music = "";
    public string MusicProgress = "";
    public bool MusicLayer1 = true;
    public bool MusicLayer2 = true;
    public bool MusicLayer3 = true;
    public bool MusicLayer4 = true;
    public string AltMusic = "";

    /// <summary>
    /// Not a real attribute, as it's actually determined by the existence of a checkpoint entity.
    /// </summary>
    public bool Checkpoint;

    internal object GetValueByName(string key) {
        return key switch {
            "name" => Name,
            "color" => C,
            "x" => X,
            "y" => Y,
            "width" => Width,
            "height" => Height,
            "cameraOffsetX" => CameraOffsetX,
            "cameraOffsetY" => CameraOffsetY,
            "windPattern" => WindPattern,
            "dark" => Dark,
            "disableDownTransition" => DisableDownTransition,
            "underwater" => Underwater,
            "checkpoint" => Checkpoint,
            "space" => Space,
            "music" => Music,
            "alt_music" => AltMusic,
            "musicProgress" => MusicProgress,
            "ambienceProgress" => AmbienceProgress,
            "musicLayer1" => MusicLayer1,
            "musicLayer2" => MusicLayer2,
            "musicLayer3" => MusicLayer3,
            "musicLayer4" => MusicLayer4,
            "whisper" => Whisper,
            "delayAltMusicFade" => DelayAltMusicFade,
            _ => throw new NotImplementedException(key)
        };
    }
    
    internal void SetValueByName(string k, object v) {
        switch (k) {
            case "name":
                Name = v.ToString() ?? "";
                break;
            case "color":
                C = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                break;
            case "x":
                X = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                break;
            case "y":
                Y = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                break;
            case "width":
                Width = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                break;
            case "height":
                Height = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                break;
            case "cameraOffsetX":
                CameraOffsetX = Convert.ToSingle(v, CultureInfo.InvariantCulture);
                break;
            case "cameraOffsetY":
                CameraOffsetY = Convert.ToSingle(v, CultureInfo.InvariantCulture);
                break;
            case "windPattern":
                WindPattern = Enum.Parse<CelesteEnums.WindPatterns>(v.ToString()!);
                break;
            case "dark":
                Dark = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "disableDownTransition":
                DisableDownTransition = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "underwater":
                Underwater = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "checkpoint":
                Checkpoint = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "space":
                Space = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "music":
                Music = v.ToString() ?? "";
                break;
            case "alt_music":
                AltMusic = v.ToString() ?? "";
                break;
            case "musicProgress":
                MusicProgress = v.ToString() ?? "";
                break;
            case "ambienceProgress":
                AmbienceProgress = v.ToString() ?? "";
                break;
            case "musicLayer1":
                MusicLayer1 = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "musicLayer2":
                MusicLayer2 = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "musicLayer3":
                MusicLayer3 = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "musicLayer4":
                MusicLayer4 = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "whisper":
                Whisper = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
            case "delayAltMusicFade":
                DelayAltMusicFade = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                break;
        }
    }
}