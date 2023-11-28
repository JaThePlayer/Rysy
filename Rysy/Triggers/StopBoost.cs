namespace Rysy.Triggers; 

[CustomEntity("stopBoostTrigger")]
public sealed class StopBoost : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("stop_boost");
}