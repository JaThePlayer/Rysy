namespace Rysy.Triggers;

[CustomEntity("birdPathTrigger")]
public sealed class BirdPath : Trigger, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("bird_path");
}