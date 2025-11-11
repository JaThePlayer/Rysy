namespace Rysy.Stylegrounds;

[CustomEntity("corestarsfg")]
public sealed class CoreStarsFg : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInBackground => false;
}