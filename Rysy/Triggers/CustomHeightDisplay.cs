namespace Rysy.Triggers; 

[CustomEntity("everest/CustomHeightDisplayTrigger")]
public sealed class CustomHeightDisplay : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        vanilla = false,
        target = 0,
        from = 0,
        text = "{x}m",
        progressAudio = false,
        displayOnTransition = false
    });

    public static PlacementList GetPlacements() => new("custom_height_display");
}