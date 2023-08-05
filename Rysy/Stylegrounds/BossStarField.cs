namespace Rysy.Stylegrounds;

[CustomEntity("bossStarField")]
public sealed class BossStarField : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
}