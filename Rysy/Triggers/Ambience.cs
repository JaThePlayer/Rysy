using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("everest/ambienceTrigger")]
public sealed class Ambience : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        track = Fields.Dropdown("", CelesteEnums.Ambience, editable: true),
        resetOnLeave = true
    });

    public static PlacementList GetPlacements() => new("ambience");
}