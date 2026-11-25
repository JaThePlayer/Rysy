using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Rysy.Components;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Scenes;
using Rysy.Shared;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;

namespace Rysy.InteropMod.InRysy;

internal sealed class RecordingDataRetriever : SceneComponent, IMenubarIndicator {
    private RecordingData? _recording;

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

    private BackgroundTaskInfo? _pollingTask;
    private Task? _inFlightPollRequest;
    
    private readonly EventRendererManager _eventRendererManager;

    private float? TargetTimestamp {
        get;
        set {
            field = value;
            _cachedRendererResults = null;
        }
    } = null;
    
    private RecordingDataRendererResults? _cachedRendererResults;

    public RecordingDataRetriever(EventRendererManager eventRendererManager) {
        _eventRendererManager = eventRendererManager;
    }

    public override void Update() {
        
    }

    private bool IsCurrentPlaybackDataValidForCurrentMap() {
        var editorState = Scene?.Get<EditorState>();
        if (_recording is null || editorState?.Map is null) {
            return false;
        }
        
        if (!editorState.Map.TryGetSid(out var sid, out var side)
            || sid != _recording.MapSid
            || (int)side != _recording.MapSide
            || editorState.Map.TryGetRoomByName(_recording.Room) is null)
            return false;

        return true;
    }

    public override void Render() {
        if (Scene is null || Opacity <= 0f)
            return;

        var editorState = Scene.Get<EditorState>();
        if (_recording is null || editorState?.Map is null) {
            return;
        }

        if (!IsCurrentPlaybackDataValidForCurrentMap())
            return;

        _cachedRendererResults ??= _eventRendererManager.HandleRecording(_recording, TargetTimestamp);

        var ctx = SpriteRenderCtx.Default();
        Gfx.BeginBatch(editorState.Camera);
        
        foreach (var sprite in _cachedRendererResults.SortedSprites) {
            sprite.Render(ctx);
        }
        
        Gfx.EndBatch();
    }

    public override void OnAdded() {
        _pollingTask ??= BackgroundTaskHelper.RegisterOnInterval(TimeSpan.FromSeconds(2), RequestNewRecordingData);
    }

    private void RequestNewRecordingData() {
        if (_inFlightPollRequest is not null && !_inFlightPollRequest.IsCompleted) {
            return;
        }
        
        if (Scene?.Get<IDebugRcClient>() is not { } client)
            return;

        _inFlightPollRequest = Task.Run(async () => {
            RecordingData? newRecording;
            try {
                var response = await client.CallAsync(DebugRcEndpoints.CurrentRecording, silent: true);
                var responseString = await response.Content.ReadAsStringAsync();

                newRecording = JsonSerializer.Deserialize<RecordingData>(responseString, NetworkingJsonOptions.IncludeFields);
            }
            catch (Exception e) {
                // Celeste isn't running to pick up the request:
                if (e is HttpRequestException {InnerException: SocketException { ErrorCode: 10061 } } )
                    return;

                Logger.Write(nameof(RecordingDataRetriever), LogLevel.Error, $"Failed to get recording from Celeste: {e}");
                return;
            }

            OnMessageReceived(newRecording);
        });
    }

    private void OnMessageReceived(RecordingData? newRecording) {
        GetSettingsFromCelesteInBackground();
        
        _recording?.Dispose();
        _recording = newRecording;
        _cachedRendererResults = null;
        var opacity = Opacity;

            /*
        lock (_spriteLock) {
            _sprites.Clear();

            foreach (var h in _recording.Holdables) {
                foreach (var f in h.Frames) {
                    var sprite = SpriteFromData(f.Position.ToXna(), _recording.SpriteData.Resolve(f.Sprite));
            
                    _sprites.Add((f.TimeStamp, sprite));
                }
            }

            _sprites.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        }
            */
    }

    public override void OnRemoved() {
    }

    public void RenderMenubarIndicator(Menubar menubar) {
        if (_inGameSettings is null)
            GetSettingsFromCelesteInBackground();
        
        var color = ThemeColors.TextColor;
        var statusTooltip = "rysy.playerTrail.connected";
        if (_recording is null) {
            color = ThemeColors.FormNullColor;
            statusTooltip = "rysy.playerTrail.disconnected";
        }
        else if (_recording is not null && !IsCurrentPlaybackDataValidForCurrentMap()) {
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

            using (_ = ScopedImGui.Disabled(_recording is null)) {
                if (ImGuiManager.TranslatedButton("rysy.playerTrail.clearPlayback")) {
                    _recording = null;
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
        
        _inGameSettingsGetTask = Task.Run(async () => {
            var response = await client.CallAsync(InGameSettings.DebugRcGetPath, silent: true);
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
