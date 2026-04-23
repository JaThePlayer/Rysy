using Rysy.Helpers;

namespace Rysy;

public sealed class RoomAttributes(BinaryPacker.Element data) {
    public BinaryPacker.Element Data { get; } = data.Clone();

    public RoomAttributes Copy() => new(Data);

    public string Name {
        get => Data.Attr("name");
        set => Data.Attributes["name"] = value;
    }

    public int X {
        get => Data.Int("x");
        set => Data.Attributes["x"] = value;
    }
    
    public int Y {
        get => Data.Int("y");
        set => Data.Attributes["y"] = value;
    }
    
    public int Width {
        get => Data.Int("width");
        set => Data.Attributes["width"] = value;
    }
    
    public int Height {
        get => Data.Int("height");
        set => Data.Attributes["height"] = value;
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
    
    public string AmbienceProgress => Data.Attr("ambienceProgress", "");
    
    public bool DisableDownTransition => Data.Bool("disableDownTransition", false);
    
    public bool Dark => Data.Bool("dark", false);
    
    public bool Whisper => Data.Bool("whisper", false);
    
    public bool Underwater => Data.Bool("underwater", false);

    public string Music => Data.Attr("music", "");
    
    public string MusicProgress => Data.Attr("musicProgress", "");
    
    public bool MusicLayer1 => Data.Bool("musicLayer1", false);
    
    public bool MusicLayer2 => Data.Bool("musicLayer2", false);
    
    public bool MusicLayer3 => Data.Bool("musicLayer3", false);
    
    public bool MusicLayer4 => Data.Bool("musicLayer4", false);
    
    public string AltMusic => Data.Attr("altMusic", "");

    /// <summary>
    /// Not a real attribute, as it's actually determined by the existence of a checkpoint entity.
    /// </summary>
    public bool Checkpoint;
}