using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("iceBlock")]
public sealed class IceBlock : RectangleEntity, IPlaceable {
    public override int Depth => -8500;

    public override Color FillColor => new(76f / 255f, 168f / 255f, 214f / 255f, 102f / 255f);

    public override Color OutlineColor => new(108f / 255f, 214f / 255f, 235f / 255f);

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("ice_block");
}