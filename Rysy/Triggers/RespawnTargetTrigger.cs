﻿namespace Rysy.Triggers;

[CustomEntity("respawnTargetTrigger")]
public class RespawnTargetTrigger : Trigger, IPlaceable {
    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new();

    public static List<Placement>? GetPlacements() => new() {
        new("Respawn Target")
    };
}
