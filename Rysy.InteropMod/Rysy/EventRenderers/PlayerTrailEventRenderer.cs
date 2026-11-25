using Microsoft.Xna.Framework;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.InteropMod.InRysy.Extensions;
using Rysy.Shared.InteropMod;
using Rysy.Shared.InteropMod.RecordingEvents;

namespace Rysy.InteropMod.InRysy.EventRenderers;

internal sealed class PlayerTrailEventRenderer : EventRenderer {
    public override string Name => "rysy.interop.eventRenderers.playerTrail";

    public override IReadOnlyList<Type> HandledEventTypes { get; } = [ typeof(PlayerTrailUpdateEvent) ];
    
    public override RenderScope CreateRenderScope(RecordingData recordingData, float? targetTimestamp) {
        return new Scope(recordingData, targetTimestamp);
    }

    private sealed class Scope(RecordingData recordingData, float? targetTimestamp) : RenderScope(targetTimestamp) {
        private readonly List<TimestampedSprite> _sprites = [];
        
        public override void HandleEvent(float eventTimestamp, RecordingData.Event nextEvent) {
            if (nextEvent is not PlayerTrailUpdateEvent f) {
                return;
            }
            
            _sprites.Add(new (eventTimestamp, recordingData.SpriteData.Resolve(f.Sprite).ToRysy(f.Position.ToXna())));
            _sprites.Add(new (eventTimestamp, ISprite.FromTexture(f.Hair.ToXna(), "characters/player/bangs00").Centered() with {
                Color =  new Color{PackedValue = f.HairColor}
            }));
        }

        public override IEnumerable<TimestampedSprite> GetSprites() {
            return _sprites;
        }

        public override void RenderImGui() {
        }
    }
}