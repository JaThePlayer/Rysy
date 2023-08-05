namespace Rysy.Stylegrounds;

[CustomEntity("dreamstars")]
public sealed class Dreamstars : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}