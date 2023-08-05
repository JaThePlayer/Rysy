namespace Rysy.Stylegrounds;

[CustomEntity("godrays")]
public sealed class GodRays : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}