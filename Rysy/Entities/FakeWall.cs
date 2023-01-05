using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fakeWall")]
public class FakeWall : TileEntity, ISolid {
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.FG;
}
