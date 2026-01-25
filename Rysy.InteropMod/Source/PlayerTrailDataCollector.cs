using Monocle;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System;
using System.Numerics;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class PlayerTrailDataCollector(OutPipeServer<PlayerTrailData> pipeServer) : IModLifetimeScoped {
    private PlayerTrailData? _currentData;
    private float _frameTimer = 1f;
    
    public void Load() {
        On.Celeste.Player.Update += Player_Update;
        Everest.Events.Player.OnDie += OnPlayerDeath;
    }

    private void OnPlayerDeath(Player player) {
        PushCurrentData();
    }

    public void Unload() {
        On.Celeste.Player.Update -= Player_Update;
        Everest.Events.Player.OnDie -= OnPlayerDeath;
    }
    
    private void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
        orig(self);

        if (pipeServer.IsConnected) {
            Update(self);
        }
    }

    private void Update(Player player) {
        var level = player.SceneAs<Level>();
        if (level?.Session is null)
            return;
        var roomName = level.Session.Level;
        var mapName = level.Session.Area.SID;

        if (_currentData is null || roomName != _currentData.Room || mapName != _currentData.MapSid) {
            PushCurrentData();
            _currentData = new PlayerTrailData {
                MapSid = mapName, 
                Room = roomName,
            };
        }

        _frameTimer -= Engine.DeltaTime;
        if (_frameTimer <= 0f) {
            _frameTimer = 2f / 60f;
            
            if (player.GetChasePosition(level.TimeActive, 0f, out Player.ChaserState chaserState)) {
                var frame = new PlayerTrailFrame {
                    Position = new Vector2(player.Position.X, player.Position.Y),
                    Animation = player.Sprite?.Texture?.AtlasPath ?? "",
                    HairColor = chaserState.HairColor.PackedValue,
                    Scale = new Vector2(chaserState.Scale.X, chaserState.Scale.Y),
                };
                
                if (_currentData.Frames is not [.., var last] || frame != last)
                    _currentData.Frames.Add(frame);
            }
        }
    }

    private void PushCurrentData() {
        if (_currentData is null) {
            return;
        }

        var data = _currentData;
        _currentData = null;
        
        pipeServer.Enqueue(data);
    }
}