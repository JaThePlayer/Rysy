using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Triggers;

[CustomEntity("bloomFadeTrigger")]
public sealed class BloomFade : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        bloomAddFrom = 0.0,
        bloomAddTo = 0.0,
        positionMode = PositionModes.NoEffect
    });

    public static PlacementList GetPlacements() => new("bloom_fade");
}