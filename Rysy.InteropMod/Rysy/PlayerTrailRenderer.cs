using Microsoft.Xna.Framework;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Scenes;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;

namespace Rysy.InteropMod.InRysy;

internal sealed class PlayerTrailRenderer : SceneComponent {
    private InPipeServer<PlaybackTrailData>? _server;

    private PlaybackTrailData? _playbackTrailData;

    private float Opacity => Scene.GetRequired<InteropModSettings>().PlaybackTrailOpacity;

    private readonly List<(float, ISprite)> _sprites = [];
    private readonly Lock _spriteLock = new();
    
    public override void Update() {
        
    }

    public override void Render() {
        var editorState = Scene.Get<EditorState>();
        if (_playbackTrailData is null || editorState?.Map is null) {
            return;
        }

        if (editorState.Map.TryGetRoomByName(_playbackTrailData.Room) is not { } room)
            return;

        var ctx = SpriteRenderCtx.Default();
        Gfx.BeginBatch(editorState.Camera);
        
        lock (_spriteLock)
            foreach (var (_, sprite) in _sprites) {
                sprite.Render(ctx);
            }
        
        Gfx.EndBatch();
    }

    public override void OnAdded() {
        _server?.Dispose();
        
        _server = new InPipeServer<PlaybackTrailData>(new Logger("Rysy.Pipes.PlayerTrailData")) {
            OnMessageReceived = OnMessageReceived
        };
        _server.Load();
    }

    private void OnMessageReceived(PlaybackTrailData obj) {
        _playbackTrailData?.Dispose();
        _playbackTrailData = obj;

        lock (_spriteLock) {
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
    }

    private ISprite SpriteFromData(Vector2 pos, SpriteData data) {
        return ISprite.FromTexture(pos, data.Texture) with {
            Scale = data.Scale.ToXna(),
            Origin = data.Origin.ToXna(),
            Rotation = data.Rotation,
            Color = new Color{PackedValue = data.Color}* Opacity,
        };
    }

    public override void OnRemoved() {
        _server?.Dispose();
        _server = null;
    }
}
