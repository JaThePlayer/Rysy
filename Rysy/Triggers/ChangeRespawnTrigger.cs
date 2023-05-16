﻿namespace Rysy.Triggers;

[CustomEntity("changeRespawnTrigger")]
internal class ChangeRespawnTrigger : Trigger, IPlaceable {
    public override Range NodeLimits => 0..1;
    public static FieldList GetFields() => new();
    public static PlacementList GetPlacements() => new() { 
        new Placement("Change Respawn")
    };
}
