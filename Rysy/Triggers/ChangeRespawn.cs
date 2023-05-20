namespace Rysy.Triggers;

[CustomEntity("changeRespawnTrigger")]
public sealed class ChangeRespawn : Trigger, IPlaceable {
    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("change_respawn");
}