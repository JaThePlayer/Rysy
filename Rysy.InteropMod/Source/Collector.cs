using Rysy.Shared.InteropMod;
using System.Collections.Generic;

namespace Celeste.Mod.Rysy.InteropMod;

/// <summary>
/// Collects in-game events.
/// </summary>
public abstract class Collector {
    protected internal abstract void LoadHooks();
    
    protected internal abstract void UnloadHooks();
    
    /// <summary>Called once every time a playback recording is started.</summary>
    protected internal abstract void BeginCollection();

    /// <summary>Called once every time a playback recording ends.</summary>
    protected internal abstract void EndCollection();

    /// <summary>
    /// Called every SampleRate to populate a Frame's Events list.
    /// Whenever this method is called, all events observed since the last time this method was called
    /// should be copied to <paramref name="into"/>, and the collector's event buffer should be cleared.
    /// </summary>
    /// <param name="currentRecording">The current recording these samples are part of.</param>
    /// <param name="into">The frame data events should be added to.</param>
    /// <returns>Whether any events got added.</returns>
    protected internal abstract bool NextSample(RecordingData currentRecording, IList<RecordingData.Event> into);
}
