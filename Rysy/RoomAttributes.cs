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
    public int CameraOffsetY;

    /// <summary>
    /// Debug color
    /// </summary>
    public int C;
    public WindPatterns WindPattern;
    public bool Space;
    public string AmbienceProgress = "";
    public bool DisableDownTransition;
    public int CameraOffsetX;
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
}
