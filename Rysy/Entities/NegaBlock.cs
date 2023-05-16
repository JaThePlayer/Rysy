using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("negaBlock")]
public sealed class NegaBlock : RectangleEntity, IPlaceable {
    public override int Depth => 0;

    public override Color FillColor => Color.Red;

    public override Color OutlineColor => Color.Red * 0.3f;

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("nega_block");
}