namespace Rysy.Triggers; 

[CustomEntity("lookoutBlocker")]
public sealed class LookoutBlocker : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("lookout_blocker");
}