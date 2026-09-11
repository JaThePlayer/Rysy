using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Rysy.Components;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Scenes;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System.Globalization;
using System.Text.Json;

namespace Rysy.InteropMod.InRysy;

internal sealed class PlayerTrailRenderer : SceneComponent, IMenubarIndicator {
    private InPipeServer<PlaybackTrailData>? _server;

    private PlaybackTrailData? _playbackTrailData;

    private float Opacity {
        get => Scene?.GetRequired<InteropModSettings>().PlaybackTrailOpacity ?? 0f;
        set {
            Scene?.GetRequired<InteropModSettings>().PlaybackTrailOpacity = value;
            InteropModModule.Instance.SaveSettings();
        }
    }

    private readonly List<(float, Sprite)> _sprites = [];
    private readonly Lock _spriteLock = new();

    private InGameSettings? _inGameSettings;
    private Task? _inGameSettingsGetTask;
    
    public override void Update() {
        
    }

    private bool IsCurrentPlaybackDataValidForCurrentMap() {
        var editorState = Scene?.Get<EditorState>();
        if (_playbackTrailData is null || editorState?.Map is null) {
            return false;
        }
        
        if (!editorState.Map.TryGetSid(out var sid, out var side)
            || sid != _playbackTrailData.MapSid
            || (int)side != _playbackTrailData.MapSide
            || editorState.Map.TryGetRoomByName(_playbackTrailData.Room) is null)
            return false;

        return true;
    }

    public override void Render() {
        if (Scene is null || Opacity <= 0f)
            return;

        var editorState = Scene.Get<EditorState>();
        if (_playbackTrailData is null || editorState?.Map is null) {
            return;
        }

        if (!IsCurrentPlaybackDataValidForCurrentMap())
            return;

        var ctx = SpriteRenderCtx.Default();
        Gfx.BeginBatch(editorState.Camera);
        
        lock (_spriteLock)
            foreach (var (_, sprite) in _sprites) {
                sprite.RenderWithColor(ctx, sprite.Color * Opacity);
            }
        
        Gfx.EndBatch();
    }

    public override void OnAdded() {
        _server?.Dispose();
        _server = null;

        if (Scene is not EditorScene)
            return;
        
        _server = new InPipeServer<PlaybackTrailData>(new Logger("Rysy.Pipes.PlayerTrailData")) {
            OnMessageReceived = OnMessageReceived
        };
        _server.Load();
    }

    private void OnMessageReceived(PlaybackTrailData obj) {
        GetSettingsFromCelesteInBackground();
        
        _playbackTrailData?.Dispose();
        _playbackTrailData = obj;
        var opacity = Opacity;

        lock (_spriteLock) {
            _sprites.Clear();
        
            foreach (var f in _playbackTrailData.Player) {
                var sprite = SpriteFromData(f.Position.ToXna(), _playbackTrailData.SpriteData.Resolve(f.Sprite));
            
                _sprites.Add((f.TimeStamp, sprite));
                _sprites.Add((f.TimeStamp, ISprite.FromTexture(f.Hair.ToXna(), "characters/player/bangs00").Centered() with {
                    Color =  new Color{PackedValue = f.HairColor}
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

    private Sprite SpriteFromData(Vector2 pos, SpriteData data) {
        return ISprite.FromTexture(pos, data.Texture) with {
            Scale = data.Scale.ToXna(),
            Origin = data.Origin.ToXna(),
            Rotation = data.Rotation,
            Color = new Color{PackedValue = data.Color},
        };
    }

    public override void OnRemoved() {
        _server?.Dispose();
        _server = null;
    }

    public void RenderMenubarIndicator(Menubar menubar) {
        if (_inGameSettings is null)
            GetSettingsFromCelesteInBackground();
        
        var color = ThemeColors.TextColor;
        var statusTooltip = "rysy.playerTrail.connected";
        if (_playbackTrailData is null && _server is null or { LikelyConnected: false }) {
            color = ThemeColors.FormNullColor;
            statusTooltip = "rysy.playerTrail.disconnected";
        }
        else if (_playbackTrailData is not null && !IsCurrentPlaybackDataValidForCurrentMap()) {
            color = ThemeColors.FormNullColor;
            statusTooltip = "rysy.playerTrail.playbackNotInThisMap";
        }
        
        if (ImGuiManager.BeginMenuIcon(ImGuiIcons.Video, color)
            .WithTranslatedTooltip("rysy.playerTrail.tooltip")
            .WithTranslatedTooltip(statusTooltip, color)) {

            float opacity = Opacity;
            if (ImGui.DragFloat("rysy.playerTrail.opacity".Translate(), ref opacity, 0.01f, 0f, 1f)
                .WithTranslatedTooltip("rysy.playerTrail.opacity.tooltip")) {
                Opacity = opacity;
            }

            float samplingInterval = _inGameSettings?.SamplingInterval ?? InGameSettings.DefaultSamplingInterval;
            if (ImGui.DragFloat("rysy.playerTrail.samplingInterval".Translate(), ref samplingInterval, 1f / 60f, 0f, 1f)
                .WithTranslatedTooltip("rysy.playerTrail.samplingInterval.tooltip")) {
                _inGameSettings ??= new InGameSettings();
                _inGameSettings.SamplingInterval = samplingInterval;
                SendSettingsToCeleste(_inGameSettings);
            }

            using (_ = ScopedImGui.Disabled(_playbackTrailData is null)) {
                if (ImGuiManager.TranslatedButton("rysy.playerTrail.clearPlayback")) {
                    _playbackTrailData = null;
                }
            }
            
            ImGui.EndMenu();
        }
    }

    private void GetSettingsFromCelesteInBackground() {
        if (Scene?.Get<IDebugRcClient>() is not { } client)
            return;
        if (_inGameSettingsGetTask is { IsCompleted: false })
            return;

        if (_server is null || !_server.LikelyConnected) {
            return;
        }
        
        _inGameSettingsGetTask = Task.Run(async () => {
            var response = await client.CallAsync(InGameSettings.DebugRcGetPath);
            var responseString = await response.Content.ReadAsStringAsync();
            
            _inGameSettings = JsonSerializer.Deserialize<InGameSettings>(responseString, NetworkingJsonOptions.IncludeFields);
        });
    }
    
    private void SendSettingsToCeleste(InGameSettings settings) {
        if (Scene?.Get<IDebugRcClient>() is not { } client)
            return;

        client.CallAsync(
            $"{InGameSettings.DebugRcSetPath}?{InGameSettings.DebugRcSetSamplingIntervalQueryString}={settings.SamplingInterval.ToString(CultureInfo.InvariantCulture)}");
    }
}
