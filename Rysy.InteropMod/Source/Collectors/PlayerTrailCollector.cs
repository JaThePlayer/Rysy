using Celeste.Mod.Rysy.InteropMod.Extensions;
using Monocle;
using Rysy.Shared.InteropMod;
using Rysy.Shared.InteropMod.RecordingEvents;
using System.Collections.Generic;
using SpriteData = Rysy.Shared.InteropMod.SpriteData;

namespace Celeste.Mod.Rysy.InteropMod.Collectors;

internal sealed class PlayerTrailCollector : Collector {
    protected internal override void LoadHooks() {
    }

    protected internal override void UnloadHooks() {
    }

    protected internal override void BeginCollection() {
    }

    protected internal override void EndCollection() {
    }

    protected internal override bool NextSample(RecordingData currentRecording, IList<RecordingData.Event> into) {
        var anyAdded = false;
        
        foreach (Player player in Engine.Scene.Tracker.GetEntities<Player>()) {
            anyAdded = true;
            into.Add(new PlayerTrailUpdateEvent {
                Position = player.Position.ToNumVec(),
                Sprite = currentRecording.SpriteData.GetKey(SpriteData.FromMonocle(player.Sprite)),
                HairColor = player.Hair.GetHairColor(0).PackedValue,
                Hair = player.Hair.Nodes[0].ToNumVec(),
            });
        }

        return anyAdded;
    }
}
