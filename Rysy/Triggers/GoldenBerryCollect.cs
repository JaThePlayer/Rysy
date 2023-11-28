namespace Rysy.Triggers; 

[CustomEntity("goldenBerryCollectTrigger")]
public sealed class GoldenBerryCollect : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("golden_berry_collection");
}