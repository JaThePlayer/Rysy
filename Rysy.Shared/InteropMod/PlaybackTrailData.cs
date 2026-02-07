using Rysy.Shared.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Rysy.Shared.InteropMod;

public sealed record PlaybackTrailData : IDisposable {
    public string MapSid { get; init; }
    
    public string Room { get; init; }

    public AutoRegistry<SpriteData> SpriteData { get; init; } = new();
    
    public PooledList<PlayerTrailFrame> Player { get; init; } = [];
    
    public PooledList<HoldableTrailData> Holdables { get; init; } = [];

    public void Dispose() {
        Player.Dispose();
        foreach (var h in Holdables) {
            h.Dispose();
        }
        Holdables.Dispose();
    }
}

public sealed record HoldableTrailData : IDisposable {
    public PooledList<HoldableFrame> Frames { get; init; } = [];
    
    public void Dispose() {
        Frames.Dispose();
    }
}

public sealed class AutoRegistry<T> : IJsonOnDeserialized where T : notnull {
    private readonly ConcurrentDictionary<T, int> _map = [];
    private int _nextId = 0;

    public ConcurrentDictionary<int, T> ResolveMap { get; init; } = [];
    
    public int GetKey(T t) {
        return _map.GetOrAdd(t, static (t, self) => {
            var id = Interlocked.Increment(ref self._nextId);
            self.ResolveMap[id] = t;
            return id;
        }, this);
    }

    public T Resolve(int id) {
        return ResolveMap[id];
    }

    public void OnDeserialized() {
        foreach (var (i, key) in ResolveMap) {
            _nextId = int.Max(_nextId, i + 1);
            _map[key] = i;
        }
    }
}

public readonly struct PlayerTrailFrame {
    [JsonPropertyName("t")]
    public float TimeStamp { get; init; }
    
    [JsonPropertyName("p")]
    public Vector2 Position { get; init; }
    
    [JsonPropertyName("s")]
    public int Sprite { get; init; }
    
    [JsonPropertyName("hc")]
    public uint HairColor { get; init; }
    
    [JsonPropertyName("h")]
    public Vector2 Hair { get; init; }
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

public readonly record struct HoldableFrame {
    [JsonPropertyName("t")]
    public float TimeStamp { get; init; }

    [JsonPropertyName("s")]
    public int Sprite { get; init; }
    
    [JsonPropertyName("p")]
    public Vector2 Position { get; init; }

    public bool Equivalent(ref HoldableFrame f) {
        return Sprite == f.Sprite && Position == f.Position;
    }
}

public readonly record struct SpriteData {
    public string Texture { get; init; }

    public Vector2 Scale { get; init; }

    public float Rotation { get; init; }
    
    public Vector2 Origin { get; init; }
    
    public uint Color { get; init; }
}