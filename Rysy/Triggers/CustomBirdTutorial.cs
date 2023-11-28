namespace Rysy.Triggers; 

[CustomEntity("everest/customBirdTutorialTrigger")]
public sealed class CustomBirdTutorial : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        birdId = "",
        showTutorial = true
    });

    public static PlacementList GetPlacements() => new("custom_bird_tutorial");
}