namespace Rysy.Stylegrounds;

[CustomEntity("stardust")]
public sealed class Stardust : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}