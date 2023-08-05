namespace Rysy.Stylegrounds;

[CustomEntity("blackhole")]
public sealed class Blackhole : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}