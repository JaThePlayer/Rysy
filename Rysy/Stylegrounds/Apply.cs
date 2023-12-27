namespace Rysy.Stylegrounds;

[CustomEntity("apply")]
public sealed class Apply : StyleFolder, IPlaceable {
    public override bool CanBeNested => false;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new();
}
