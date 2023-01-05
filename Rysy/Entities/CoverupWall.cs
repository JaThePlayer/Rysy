using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("coverupWall")]
public class CoverupWall : TileEntity, ISolid {
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.FG;
}
