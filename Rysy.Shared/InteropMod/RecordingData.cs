using Rysy.Shared.Collections;
using Rysy.Shared.InteropMod.RecordingEvents;
using System.Text.Json.Serialization;

namespace Rysy.Shared.InteropMod;

/// <summary>
/// A full recording, sent as json over to Rysy.
/// </summary>
public sealed class RecordingData : IDisposable {
    public string MapSid { get; init; }
    public int MapSide { get; init; }
    public string Room { get; init; }
    
    public float FinalTimestamp { get; set; }
    
    /// <summary>Whether the recording is finished, or is still in-progress.</summary>
    public bool Finished { get; set; }
    
    /// <summary>A cache used to deduplicate repeating sprites in the json file sent to Rysy.</summary>
    /// TODO: Solve multithreading issue when sending a RecordingData to rysy while it is being updated.
    public AutoRegistry<SpriteData> SpriteData { get; init; } = new();
    
    /// <summary>List with all sampled frame data. A frame should be added to this list every SamplingRate (unless the frame has no events).</summary>
    public PooledList<Frame> Samples { get; init; } = new();

    public struct Frame {
        public Frame(float timestamp) {
            Timestamp = timestamp;
        }

        /// <summary>
        /// The timestamp of this frame, relative to the start of the recording.
        /// The timestamp should be incremented by Engine.DeltaTime each frame, even if there's no sampling that frame.
        /// This way, even when Frames are omitted or SamplingRate is changed,
        /// Rysy can still reconstruct when events actually happened and display them correctly in the timeline.
        /// </summary>
        public float Timestamp { get; set; }

        /// <summary>
        /// List of events gathered this in-game frame.
        /// Frames without any Events should be omitted from PlaybackData's `Samples` list entirely.
        /// </summary>
        public PooledList<Event> Events { get; set;  } = [];
    }

    /// <summary>
    /// Some event that happened during a frame.
    /// For the player trail collector, this would be the player position,
    /// for a hitbox collector this would be all hitboxes changed since last sample, and so on.
    /// </summary>
    /// TODO: Swap away from builtin inheritance impl, to allow plugins to extend this!
    [JsonDerivedType(typeof(PlayerTrailUpdateEvent), typeDiscriminator: "ptu")]
    public abstract class Event;

    public void Dispose() {
        foreach (var frame in Samples) {
            frame.Events.Dispose();
        }
        Samples.Dispose();
    }
}
