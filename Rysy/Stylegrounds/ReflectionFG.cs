namespace Rysy.Stylegrounds;

[CustomEntity("reflectionfg")]
public sealed class ReflectionFg : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInBackground => false;
}