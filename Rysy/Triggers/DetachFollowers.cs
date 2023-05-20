namespace Rysy.Triggers;

[CustomEntity("detachFollowersTrigger")]
public sealed class DetachFollowers : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        global = true
    });

    public static PlacementList GetPlacements() => new("detach_followers");
}