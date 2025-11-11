using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("finalBossFallingBlock")]
public class BadelineBossFallingBlock : TileEntity, IPlaceable {
    public override char Tiletype => 'G';

    public override TileLayer Layer => TileLayer.Fg;

    public override int Depth => 0;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("falling_block");
}
