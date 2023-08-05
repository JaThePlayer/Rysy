namespace Rysy.Stylegrounds;

[CustomEntity("stars")]
public sealed class Stars : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}