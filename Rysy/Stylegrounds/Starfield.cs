namespace Rysy.Stylegrounds;

[CustomEntity("starfield")]
public sealed class Starfield : Style, IPlaceable {
    public static FieldList GetFields() => new(new {
        color = Fields.Rgb(null!).AllowNull(),
        speed = 1.0f,
        scrollx = 1.0f,
        scrolly = 1.0f,
    });

    public static PlacementList GetPlacements() => new("default");
}