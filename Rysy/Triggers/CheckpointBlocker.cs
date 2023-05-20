namespace Rysy.Triggers;

[CustomEntity("checkpointBlockerTrigger")]
public sealed class CheckpointBlocker : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("checkpoint_blocker");
}