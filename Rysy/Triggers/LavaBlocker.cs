namespace Rysy.Triggers; 

[CustomEntity("everest/lavaBlockerTrigger")]
public sealed class LavaBlocker : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        canReenter = false
    });

    public static PlacementList GetPlacements() => new("lava_blocker");
}