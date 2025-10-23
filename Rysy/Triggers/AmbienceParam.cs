using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Triggers;

[CustomEntity("ambienceParamTrigger")]
public sealed class AmbienceParam : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Audio;
    
    public static FieldList GetFields() => new(new {
        parameter = "",
        from = 0.0,
        to = 0.0,
        direction = PositionModes.NoEffect,
    });

    public static PlacementList GetPlacements() => new("ambience_param");
}