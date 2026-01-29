using Rysy.Shared.Collections;
using System.Numerics;

namespace Rysy.Shared.InteropMod;

public sealed record PlayerTrailData : IDisposable {
    public string MapSid { get; init; }
    
    public string Room { get; init; }

    public PooledList<PlayerTrailFrame> Frames { get; init; } = [];

    public void Dispose() {
        Frames.Dispose();
    }
}

public readonly record struct PlayerTrailFrame {
    public float TimeStamp { get; init; }
    
    public Vector2 Position { get; init; }
    
    public string Animation { get; init; }
    
    public uint HairColor { get; init; }
    
    public Vector2 Scale { get; init; }
}
