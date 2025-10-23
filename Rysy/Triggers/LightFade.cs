using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("lightFadeTrigger")]
public sealed class LightFade : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Visual;

    public static FieldList GetFields() => new(new {
        lightAddFrom = 0.0,
        lightAddTo = 0.0,
        positionMode = CelesteEnums.PositionModes.NoEffect,
    });

    public static PlacementList GetPlacements() => new("light_fade");
}