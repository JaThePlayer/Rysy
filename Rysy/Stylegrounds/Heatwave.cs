namespace Rysy.Stylegrounds;

[CustomEntity("heatwave")]
public sealed class Heatwave : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}