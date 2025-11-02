namespace Rysy.Triggers; 

[CustomEntity("lookoutBlocker")]
public sealed class LookoutBlocker : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => [
        new Placement("watchtower_blocker") {
            AlternativeNames = [
                "binocular_blocker",
                "lookout_blocker",
            ],
        }
    ];
}