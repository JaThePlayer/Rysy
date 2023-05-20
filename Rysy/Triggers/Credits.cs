namespace Rysy.Triggers;

[CustomEntity("creditsTrigger")]
public sealed class Credits : Trigger, IPlaceable {
    public static FieldList GetFields() => new() {
        ["event"] = Fields.String("")
    };

    public static PlacementList GetPlacements() => new("credits");
}