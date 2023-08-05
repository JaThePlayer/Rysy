namespace Rysy.Stylegrounds;

[CustomEntity("petals")]
public sealed class Petals : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}