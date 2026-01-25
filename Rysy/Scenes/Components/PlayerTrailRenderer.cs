using Rysy.Graphics;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;

namespace Rysy.Scenes.Components;

internal sealed class PlayerTrailRenderer(EditorScene scene) : SceneComponent {
    private InPipeServer<PlayerTrailData>? _server;

    private PlayerTrailData? _playerTrailData;
    
    public override void Update() {
        
    }

    public override void Render() {
        if (_playerTrailData is null || scene.Map is null) {
            return;
        }

        if (scene.Map.TryGetRoomByName(_playerTrailData.Room) is not { } room)
            return;

        var ctx = SpriteRenderCtx.Default();
        Gfx.BeginBatch(scene.Camera);
        
        foreach (var f in _playerTrailData.Frames) {
            var sprite = ISprite.FromTexture(f.Position.ToXna(), f.Animation) with {
                Scale = f.Scale.ToXna(),
                Origin = new(.5f, 1f),
            };
                
            sprite.RenderWithColor(ctx, new Color{PackedValue = f.HairColor} * 0.7f);
        }
        
        Gfx.EndBatch();
    }

    public override void OnBegin() {
        _server?.Dispose();
        
        _server = new InPipeServer<PlayerTrailData>(new Logger("Rysy.Pipes.PlayerTrailData")) {
            OnMessageReceived = OnMessageReceived
        };
        _server.Load();
    }

    private void OnMessageReceived(PlayerTrailData obj) {
        _playerTrailData = obj;
    }

    public override void OnEnd() {
        _server?.Dispose();
        _server = null;
    }
}
