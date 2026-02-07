using Monocle;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SpriteData = Rysy.Shared.InteropMod.SpriteData;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class PlayerTrailDataCollector(OutPipeServer<PlaybackTrailData> pipeServer) : IModLifetimeScoped {
    private PlaybackTrailData? _currentData;

    private const float RespawnDelay = 1f;
    
    private float _frameTimer = RespawnDelay;
    private float _timestamp = 0f;

    private ConditionalWeakTable<Holdable, HoldableTrailData> _holdableTrails = [];
    
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
            _currentData = new PlaybackTrailData {
                MapSid = mapName, 
                Room = roomName,
            };
        }

        _frameTimer -= Engine.DeltaTime;
        _timestamp += Engine.DeltaTime;
        if (_frameTimer <= 0f) {
            _frameTimer = 8f / 60f;
            
            if (player.GetChasePosition(level.TimeActive, 0f, out Player.ChaserState chaserState)) {
                var frame = new PlayerTrailFrame {
                    TimeStamp = _timestamp,
                    Position = new Vector2(player.Position.X, player.Position.Y),
                    Sprite = _currentData.SpriteData.GetKey(CreateDataFromSprite(player.Sprite)),
                    HairColor = player.Hair.GetHairColor(0).PackedValue,
                    Hair = player.Hair.Nodes[0].ToNumVec(),
                };
                
                if (_currentData.Player is not [.., var last] || !frame.Equivalent(ref last))
                    _currentData.Player.Add(frame);
            }

            foreach (Holdable h in level.Tracker.GetComponents<Holdable>()) {
                var sprite = h.Entity.Get<Sprite>();
                if (sprite is null)
                    continue;
                
                var trail = _holdableTrails.GetOrCreateValue(h);

                var frame = new HoldableFrame {
                    TimeStamp = _timestamp,
                    Sprite = _currentData.SpriteData.GetKey(CreateDataFromSprite(sprite)),
                    Position = sprite.RenderPosition.ToNumVec(),
                };
                
                if (trail.Frames is not [.., var last] || !frame.Equivalent(ref last))
                    trail.Frames.Add(frame);
            }
        }
    }
    
    /*
    private Vector2 GetHairScale(int hairCount, int index)
    {
        float num = 0.25f + (1f - index / (float)hairCount) * 0.75f;
        return new Vector2(((index == 0) ? ((float)this.Facing) : num) * Math.Abs(this.Sprite.Scale.X), num);
    }
    */

    private SpriteData CreateDataFromSprite(Sprite spr) {
        return new() {
            Rotation = spr.Rotation,
            Origin = (spr.Origin / new Microsoft.Xna.Framework.Vector2(spr.Texture.Width, spr.Texture.Height)).ToNumVec(),
            Scale = spr.Scale.ToNumVec(),
            Texture = spr.Texture.AtlasPath ?? "",
            Color = spr.Color.PackedValue,
        };
    }
    
    private void PushCurrentData() {
        if (_currentData is null) {
            return;
        }

        var data = _currentData;
        foreach (var (h, holdableTrailData) in _holdableTrails) {
            data.Holdables.Add(holdableTrailData);
        }
        _currentData = null;
        _timestamp = 0f;
        _frameTimer = RespawnDelay;
        _holdableTrails.Clear();
        
        pipeServer.Enqueue(data);
    }
}