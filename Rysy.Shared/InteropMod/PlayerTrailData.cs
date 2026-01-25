using System.Numerics;

namespace Rysy.Shared.InteropMod;

public record class PlayerTrailData {
    public string MapSid { get; init; }
    
    public string Room { get; init; }

    public List<PlayerTrailFrame> Frames { get; init; } = [];
}

public readonly record struct PlayerTrailFrame {
    public float TimeStamp { get; init; }
    
    public Vector2 Position { get; init; }
    
    public string Animation { get; init; }
    
    public uint HairColor { get; init; }
    
    public Vector2 Scale { get; init; }
}
