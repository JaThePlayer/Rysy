namespace Rysy;

public class RoomAttributes {
    public bool DelayAltMusicFade { get; set; }
    public int CameraOffsetY { get; set; }

    /// <summary>
    /// Debug color
    /// </summary>
    public int C { get; set; }
    public string WindPattern { get; set; }
    public bool Space { get; set; }
    public string AmbienceProgress { get; set; }
    public bool DisableDownTransition { get; set; }
    public int CameraOffsetX { get; set; }
    public bool Dark { get; set; }
    public bool Whisper { get; set; }
    public bool Underwater { get; set; }

    public string Music { get; set; }
    public string MusicProgress { get; set; }
    public bool MusicLayer1 { get; set; }
    public bool MusicLayer2 { get; set; }
    public bool MusicLayer3 { get; set; }
    public bool MusicLayer4 { get; set; }
    public string AltMusic { get; set; }
}
