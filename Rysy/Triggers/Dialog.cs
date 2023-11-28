namespace Rysy.Triggers; 

[CustomEntity("everest/dialogTrigger")]
public sealed class Dialog : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        endLevel = false,
        onlyOnce = true,
        dialogId = "",
        deathCount = -1
    });

    public static PlacementList GetPlacements() => new("dialog");
}