namespace Rysy.Stylegrounds;

[CustomEntity("northernlights")]
public sealed class NorthernLights : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}