using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("glassBlock")]
public sealed class GlassBlock : RectangleEntity, IPlaceable {
    public override Color FillColor => Color.White * 0.6f;

    public override Color OutlineColor => Color.White * 0.8f;

    public override int Depth => 0;

    public static FieldList GetFields() => new(new {
        sinks = false,
    });

    public static PlacementList GetPlacements() => new("glass_block");
}
