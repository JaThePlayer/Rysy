using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lightning")]
public class Lightning : RectangleEntity, IPlaceable {
    private static readonly Color outlineColor = "fcf579".FromRGB();
    private static readonly Color color = outlineColor * .3f;

    public override Color FillColor => color;

    public override Color OutlineColor => outlineColor;

    public override int Depth => -1000100;

    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new(new {
        perLevel = false,
        moveTime = 5.0f
    });

    public static PlacementList GetPlacements() => new("lightning");

    public override bool CanTrim(string key, object val)
        => IsDefault(key, val);
}
