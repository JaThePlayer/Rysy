namespace Rysy.Triggers;

[CustomEntity("everest/completeAreaTrigger")]
public sealed class CompleteArea : Trigger, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("complete_area");
}