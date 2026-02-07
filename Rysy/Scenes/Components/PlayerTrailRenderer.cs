using Rysy.Graphics;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;

namespace Rysy.Scenes.Components;

internal sealed class PlayerTrailRenderer(EditorScene scene) : SceneComponent {
    private InPipeServer<PlaybackTrailData>? _server;

    private PlaybackTrailData? _playbackTrailData;

    private float Opacity => Settings.Instance?.PlaytestTrailOpacity ?? 0.45f;

    private readonly List<(float, ISprite)> _sprites = [];
    
    public override void Update() {
        
    }

    public override void Render() {
        if (_playbackTrailData is null || scene.Map is null) {
            return;
        }

        if (scene.Map.TryGetRoomByName(_playbackTrailData.Room) is not { } room)
            return;

        var ctx = SpriteRenderCtx.Default();
        Gfx.BeginBatch(scene.Camera);
        
        foreach (var (_, sprite) in _sprites) {
            sprite.Render(ctx);
        }
        
        Gfx.EndBatch();
    }

    public override void OnBegin() {
        _server?.Dispose();
        
        _server = new InPipeServer<PlaybackTrailData>(new Logger("Rysy.Pipes.PlayerTrailData")) {
            OnMessageReceived = OnMessageReceived
        };
        _server.Load();
    }

    private void OnMessageReceived(PlaybackTrailData obj) {
        _playbackTrailData?.Dispose();
        _playbackTrailData = obj;
        
        _sprites.Clear();
        
        foreach (var f in _playbackTrailData.Player) {
            var sprite = SpriteFromData(f.Position.ToXna(), _playbackTrailData.SpriteData.Resolve(f.Sprite));
            
            _sprites.Add((f.TimeStamp, sprite));
            _sprites.Add((f.TimeStamp, ISprite.FromTexture(f.Hair.ToXna(), "characters/player/bangs00").Centered() with {
                Color =  new Color{PackedValue = f.HairColor} * Opacity
            }));
        }

        foreach (var h in _playbackTrailData.Holdables) {
            foreach (var f in h.Frames) {
                var sprite = SpriteFromData(f.Position.ToXna(), _playbackTrailData.SpriteData.Resolve(f.Sprite));
            
                _sprites.Add((f.TimeStamp, sprite));
            }
        }

        _sprites.Sort((a, b) => a.Item1.CompareTo(b.Item1));
    }

    private ISprite SpriteFromData(Vector2 pos, SpriteData data) {
        return ISprite.FromTexture(pos, data.Texture) with {
            Scale = data.Scale.ToXna(),
            Origin = data.Origin.ToXna(),
            Rotation = data.Rotation,
            Color = new Color{PackedValue = data.Color}* Opacity,
        };
    }

    public override void OnEnd() {
        _server?.Dispose();
        _server = null;
    }
}
