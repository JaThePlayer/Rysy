using System.Numerics;
using System.Text.Json.Serialization;

namespace Rysy.Shared.InteropMod.RecordingEvents;

public sealed class PlayerTrailUpdateEvent : RecordingData.Event {
    [JsonPropertyName("p")]
    public required Vector2 Position { get; init; }
    
    [JsonPropertyName("s")]
    public required int Sprite { get; init; }
    
    [JsonPropertyName("hc")]
    public required uint HairColor { get; init; }
    
    [JsonPropertyName("h")]
    public required Vector2 Hair { get; init; }
    /*

    [JsonPropertyName("t")]
    public string Animation { get; init; }

    public uint HairColor { get; init; }

    public Vector2 Scale { get; init; }
    */

    public bool Equivalent(ref PlayerTrailFrame f) {
        return Position == f.Position 
               && Sprite == f.Sprite
               && HairColor == f.HairColor;
    }
}