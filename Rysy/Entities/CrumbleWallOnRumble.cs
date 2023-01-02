using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("crumbleWallOnRumble")]
public class CrumbleWallOnRumble : TileEntity, ISolid
{
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", 'm');

    public override TileLayer Layer => TileLayer.FG;
}
