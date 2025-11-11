using Rysy.Graphics;
using Rysy.Mods;

namespace Rysy.Helpers;

public static class PlaybackRegistry {
    private static Dictionary<string, List<ChaserState>> Tutorials = new();

    public static List<ChaserState>? GetTutorial(string name) {
        lock (Tutorials) {
            if (Tutorials.TryGetValue(name, out var cached))
                return cached;
        }

        var exists = ModRegistry.Filesystem.TryWatchAndOpen($"Tutorials/{name}.bin", stream => {
            var data = Read(stream);

            lock (Tutorials) {
                Tutorials[name] = data;
            }
        });

        if (exists) {
            return Tutorials[name];
        }

        return null;
    }

    public static IEnumerable<ISprite> GetSprites(Vector2 pos, string tutorial) {
        var data = GetTutorial(tutorial);
        if (data is null)
            yield break;

        var anim = "";
        var bank = EditorState.Map?.Sprites;
        if (bank is null) {
            yield break;
        }

        Vector2? lastPos = null;
        bool skip = true;
        foreach (var state in data) {
            skip = !skip;

            if (skip)
                continue;

            if (bank.Exists("player_playback", state.Animation, Gfx.Atlas)) {
                anim = state.Animation;
            }

            if (string.IsNullOrWhiteSpace(anim) || state.Position == lastPos) {
                continue;
            }

            yield return ISprite.FromSpriteBank(pos + state.Position, "player_playback", anim) with {
                Color = state.HairColor * 0.3f,
                //Scale = state.Scale,
                Origin = new(0.5f, 1f),
            };

            lastPos = state.Position;
        }
    }

    public enum Facings {
        Right = 1,
        Left = -1
    }

    public record struct ChaserState {
        public Vector2 Position;

        public float TimeStamp;

        public string Animation;

        public Facings Facing;

        public bool OnGround;

        public Color HairColor;

        public int Depth;

        public Vector2 Scale;

        public Vector2 DashDirection;
    }

    public static List<ChaserState> Read(Stream stream) {
        List<ChaserState> list = new();

        using var reader = new BinaryReader(stream);

        int version = 1;
        if (reader.ReadString() == "TIMELINE") {
            version = reader.ReadInt32();
        } else {
            reader.BaseStream.Seek(0L, SeekOrigin.Begin);
        }
        int stateAmt = reader.ReadInt32();
        for (int i = 0; i < stateAmt; i++) {
            ChaserState state = new();
            state.Position.X = reader.ReadSingle();
            state.Position.Y = reader.ReadSingle();
            state.TimeStamp = reader.ReadSingle();
            state.Animation = reader.ReadString();
            state.Facing = (Facings) reader.ReadInt32();
            state.OnGround = reader.ReadBoolean();
            state.HairColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), (byte) 255);
            state.Depth = reader.ReadInt32();

            if (version == 1) {
                state.Scale = new Vector2((float) state.Facing, 1f);
                state.DashDirection = Vector2.Zero;
            } else {
                state.Scale.X = reader.ReadSingle();
                state.Scale.Y = reader.ReadSingle();
                state.DashDirection.X = reader.ReadSingle();
                state.DashDirection.Y = reader.ReadSingle();
            }
            list.Add(state);
        }

        return list;
    }
}
