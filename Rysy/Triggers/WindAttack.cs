namespace Rysy.Triggers; 

[CustomEntity("windAttackTrigger")]
public sealed class WindAttack : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("wind_attack");
}