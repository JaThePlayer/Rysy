using Monocle;
using Rysy.Shared.InteropMod;
using System.Collections.Generic;

namespace Celeste.Mod.Rysy.InteropMod;

public sealed class RecordingManager : IModLifetimeScoped {
    private readonly List<Collector> _collectors = [];
    
    public float SamplingInterval => InteropModModule.Settings.RemoteSettings.SamplingInterval;
    
    /// <summary>Whenever a recording finishes, populate this property and reset CurrentRecording.</summary>
    public RecordingData? LatestFinishedRecording { get; private set; }
    
    public RecordingData? CurrentRecording { get; private set; }

    // Polled by Rysy via DebugRC.
    public RecordingData? GetRecordingDataForRysy() {
        // If a recording was just finished (for example, the player died),
        // and the finished recording was not yet polled by Rysy, send the finished recording,
        // so Rysy cannot accidentally skip over the finished recording.
        if (LatestFinishedRecording is {} latest) {
            LatestFinishedRecording = null;
            return latest;
        }
        
        // Depending on Rysy settings this might return null instead to reduce cpu/gc overhead.
        return CurrentRecording;
    }

    public RecordingData? FinishCurrentRecording() {
        if (CurrentRecording is null) {
            return null;
        }

        CurrentRecording.Finished = true;
        LatestFinishedRecording = CurrentRecording;
        CurrentRecording = null;

        foreach (var collector in _collectors) {
            collector.EndCollection();
        }
        
        return LatestFinishedRecording;
    }

    public void BeginNewRecording(Level level) {
        FinishCurrentRecording();
        
        CurrentRecording = new RecordingData {
            Room = level.Session.Level,
            MapSid = level.Session.Area.SID,
            MapSide = (int) level.Session.Area.Mode
        };
        
        foreach (var collector in _collectors) {
            collector.BeginCollection();
        }
    }

    public RecordingManager RegisterCollector(Collector collector) {
        _collectors.Add(collector);
        collector.LoadHooks();

        return this;
    }
    
    
    private void OnEndOfFrame(Level level) {
        if (CurrentRecording is null)
            return;
        
        var roomName = level.Session.Level;
        var mapName = level.Session.Area.SID;
        var mapSide = level.Session.Area.Mode;

        if (roomName != CurrentRecording.Room || mapName != CurrentRecording.MapSid || (int)mapSide != CurrentRecording.MapSide) {
            BeginNewRecording(level);
        }

        CurrentRecording.FinalTimestamp += Engine.DeltaTime;
        if (OnInterval(CurrentRecording.FinalTimestamp, SamplingInterval)) {
            var frame = new RecordingData.Frame(CurrentRecording.FinalTimestamp);

            foreach (var collector in _collectors) {
                collector.NextSample(CurrentRecording, frame.Events);
            }

            if (frame.Events.Count <= 0) {
                //frame.Events.Dispose();
            } else {
                CurrentRecording.Samples.Add(frame);
            }
        }
    }
    
    private static bool OnInterval(float time, float interval)
    {
        return (int)((time - Engine.DeltaTime) / interval) < (int)(time / interval);
    }
    
    private void OnPlayerDeath(Player player) {
        FinishCurrentRecording();
    }

    private void OnPlayerSpawn(Player player) {
        if (player.Scene is Level level) {
            BeginNewRecording(level);
        }
    }
    
    void IModLifetimeScoped.Load() {
        Everest.Events.Level.OnAfterUpdate += OnEndOfFrame;
        Everest.Events.Player.OnDie += OnPlayerDeath;
        Everest.Events.Player.OnSpawn += OnPlayerSpawn;
    }


    void IModLifetimeScoped.Unload() {
        Everest.Events.Level.OnAfterUpdate -= OnEndOfFrame;
        Everest.Events.Player.OnDie -= OnPlayerDeath;
        Everest.Events.Player.OnSpawn -= OnPlayerSpawn;

        foreach (var collector in _collectors) {
            collector.UnloadHooks();
        }
        _collectors.Clear();
    }
}