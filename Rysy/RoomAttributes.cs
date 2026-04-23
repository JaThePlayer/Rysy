using Rysy.Helpers;

namespace Rysy;

public sealed class RoomAttributes(BinaryPacker.Element data) {
    public EntityData Data { get; } = new(data.Name ?? "room", data);

    public RoomAttributes Copy() => new(new BinaryPacker.Element { Attributes = Data.Inner });

    public string Name {
        get => Data.Attr("name");
        set => Data["name"] = value;
    }

    public int X {
        get => Data.Int("x");
        set => Data["x"] = value;
    }
    
    public int Y {
        get => Data.Int("y");
        set => Data["y"] = value;
    }
    
    public int Width {
        get => Data.Int("width");
        set => Data["width"] = value;
    }
    
    public int Height {
        get => Data.Int("height");
        set => Data["height"] = value;
    }

    public bool DelayAltMusicFade => Data.Bool("delayAltMusicFade", false);

    public float CameraOffsetX => Data.Int("cameraOffsetX", 0);
    public float CameraOffsetY => Data.Int("cameraOffsetY", 0);

    /// <summary>
    /// Debug color
    /// </summary>
    public int C => Data.Int("c", 0);

    public CelesteEnums.WindPatterns WindPattern => Data.Enum("windPattern", CelesteEnums.WindPatterns.None);
    
    public bool Space => Data.Bool("space", false);
    
    public bool DisableDownTransition => Data.Bool("disableDownTransition", false);
    
    public bool Dark => Data.Bool("dark", false);
    
    public bool Whisper => Data.Bool("whisper", false);
    
    public bool Underwater => Data.Bool("underwater", false);

    public string Music => Data.Attr("music", "");

    public string Ambience => Data.Attr("ambience");
    
    public int AmbienceProgress => Data.Int("ambienceProgress", -1);
    
    public int MusicProgress => Data.Int("musicProgress", -1);
    
    public bool MusicLayer1 => Data.Bool("musicLayer1", false);
    
    public bool MusicLayer2 => Data.Bool("musicLayer2", false);
    
    public bool MusicLayer3 => Data.Bool("musicLayer3", false);
    
    public bool MusicLayer4 => Data.Bool("musicLayer4", false);
    
    public string AltMusic => Data.Attr("alt_music", "");

    /// <summary>
    /// Not a real attribute, as it's actually determined by the existence of a checkpoint entity.
    /// </summary>
    public bool Checkpoint;
}