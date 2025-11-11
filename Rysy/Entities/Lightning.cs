using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lightning")]
public class Lightning : RectangleEntity, IPlaceable {
    public override Color FillColor => OutlineColor * 0.3f;

    public override Color OutlineColor => "fcf579".FromRgb();

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
