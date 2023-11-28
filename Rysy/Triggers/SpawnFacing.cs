using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("spawnFacingTrigger")]
public sealed class SpawnFacing : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        facing = CelesteEnums.SpawnFacings.Right
    });

    public static PlacementList GetPlacements() => new("spawn_facing");
}