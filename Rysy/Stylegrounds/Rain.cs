namespace Rysy.Stylegrounds;

[CustomEntity("rain")]
public sealed class Rain : Style, IPlaceable {
    public static FieldList GetFields() => new(new {
        color = Fields.Rgb("161933").AllowNull(),
    });

    public static PlacementList GetPlacements() => new("default");
}