using Rysy.Graphics;
using Rysy.Shared.InteropMod;

namespace Rysy.InteropMod.InRysy;

public sealed class EventRendererManager {
    /// <summary>
    /// Stores which event renderers are interested in a particular event type.
    /// </summary>
    private readonly Dictionary<Type, List<EventRenderer>> _renderersByEventType = [];
    private readonly List<EventRenderer> _renderers = [];

    public EventRendererManager RegisterRenderer(EventRenderer renderer) {
        _renderers.Add(renderer);
        foreach (var rendererType in renderer.HandledEventTypes) {
            if (!_renderersByEventType.TryGetValue(rendererType, out var renderers)) {
                _renderersByEventType[rendererType] = renderers = [];
            }
            
            renderers.Add(renderer);
        }

        return this;
    }
    
    // Called once upon receiving a recording data from the game, or when the user demands rendering a different timestamp.
    // Scrolling through the playback could be optimised by keeping around a clone of each RenderScope after fully processing each Sample,
    // so you would have a ready list of timestamp->rendering instructions to search through as needed.
    public RecordingDataRendererResults HandleRecording(RecordingData recordingData, float? targetTimestamp) {
        Dictionary<EventRenderer, EventRenderer.RenderScope> scopes = _renderers
            .ToDictionary(r => r, r => r.CreateRenderScope(recordingData, targetTimestamp));
        
        Dictionary<Type, List<EventRenderer.RenderScope>> scopesByType =
            _renderersByEventType.ToDictionary(kv => kv.Key, kv => kv.Value.Select(r => scopes[r]).ToList());

        foreach (var frame in recordingData.Samples) {
            if (targetTimestamp.HasValue && frame.Timestamp > targetTimestamp.Value)
                break;
            
            foreach (var frameEvent in frame.Events) {
                scopesByType.GetValueOrDefault(frameEvent.GetType())?.ForEach(s => s.HandleEvent(frame.Timestamp, frameEvent));
            }
        }
        
        // collect results from scopes, sort sprites by depth.
        var sprites = scopes.SelectMany(kv => kv.Value.GetSprites())
            .OrderBy(s => s.Timestamp)
            .ThenBy(s => s.Sprite.Depth)
            .Select(s => s.Sprite)
            .ToList();

        return new() {
            TargetTimestamp = targetTimestamp,
            SortedSprites = sprites,
            RenderScopes = scopes.Values.ToList(),
        };
    }
}

public sealed class RecordingDataRendererResults {
    public required float? TargetTimestamp { get; init; }
    
    public required List<ISprite> SortedSprites { get; init; }
    
    public required IReadOnlyList<EventRenderer.RenderScope> RenderScopes { get; init; }
}
