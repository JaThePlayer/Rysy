using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("windTrigger")]
public sealed class Wind : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        pattern = CelesteEnums.WindPatterns.None,
    });

    public static PlacementList GetPlacements() => new("wind");
}