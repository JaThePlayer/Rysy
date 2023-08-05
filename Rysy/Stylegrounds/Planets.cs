using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Stylegrounds;

[CustomEntity("planets")]
public sealed class Planets : Style, IPlaceable {
    public static FieldList GetFields() => new(new {
        count = 32,
        size = PlanetStyleSizes.Small,
        color = Fields.RGB(null!).AllowNull(),
        scrollx = 1.0,
        scrolly = 1.0
    });

    public static PlacementList GetPlacements() => new("default");
}