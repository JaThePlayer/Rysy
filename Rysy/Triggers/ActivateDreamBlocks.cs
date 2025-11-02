namespace Rysy.Triggers;

[CustomEntity("everest/activateDreamBlocksTrigger")]
public sealed class ActivateDreamBlocks : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        fullRoutine = false,
        activate = true,
        fastAnimation = false
    });

    public static PlacementList GetPlacements() => [
        new Placement("activate_dream_blocks") {
            AlternativeNames = [ "activate_space_jam" ]
        }
    ];
}