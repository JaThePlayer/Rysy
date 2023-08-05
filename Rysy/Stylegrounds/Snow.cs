namespace Rysy.Stylegrounds;

[CustomEntity("snowFg")]
public sealed class SnowFG : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInBackground => false;
}

[CustomEntity("snowBg")]
public sealed class SnowBG : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInForeground => false;
}

[CustomEntity("windSnow")]
public sealed class WindSnow : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override bool CanBeInForeground => false;
}