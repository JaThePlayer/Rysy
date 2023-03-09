namespace Rysy.Triggers;

[CustomEntity("changeRespawnTrigger")]
internal class ChangeRespawnTrigger : Trigger, IPlaceable {
    public static List<Placement>? GetPlacements() => new List<Placement> { 
        new Placement("Change Respawn")
    };
}
