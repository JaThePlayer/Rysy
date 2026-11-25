using Rysy.Graphics;
using Rysy.Shared.InteropMod;

namespace Rysy.InteropMod.InRysy;

public abstract class EventRenderer {
    // Lang key to display to the user in filter UI.
    public abstract string Name { get; }
    
    public abstract IReadOnlyList<Type> HandledEventTypes { get; }

    public abstract RenderScope CreateRenderScope(RecordingData recordingData, float? targetTimestamp);
    
    // Holds data needed to process rendering up to the given timestamp (or all events if no timestamp provided)
    public abstract class RenderScope(float? targetTimestamp) {
        public float? TargetTimestamp { get; } = targetTimestamp;

        public abstract void HandleEvent(float eventTimestamp, RecordingData.Event nextEvent);

        // The following are only called after all HandleEvent calls:
        
        /// <summary>
        /// Gets all the sprites from this renderer along with the timestamp they were created at.
        /// </summary>
        public abstract IEnumerable<TimestampedSprite> GetSprites();

        public abstract void RenderImGui();
    }
}

public record struct TimestampedSprite(float Timestamp, ISprite Sprite);