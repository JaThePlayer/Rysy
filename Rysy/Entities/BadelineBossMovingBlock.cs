using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("finalBossMovingBlock")]
internal sealed class BadelineBossMovingBlock : TileEntity, IPlaceable {
    public override char Tiletype => 'G';

    public override TileLayer Layer => TileLayer.Fg;

    public override int Depth => 0;

    public override Range NodeLimits => 1..1;

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex)
        => GetSprites(Nodes[nodeIndex], Layer, 'g');

    public static FieldList GetFields() => new(new {
        nodeIndex = Fields.Int(0).WithRange(0..),
    });

    public static PlacementList GetPlacements() => new("moving_block");

}
