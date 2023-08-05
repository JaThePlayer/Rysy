namespace Rysy.Stylegrounds;

[CustomEntity("mirrorfg")]
public sealed class MirrorFG : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInBackground => false;
}