using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("crumbleWallOnRumble")]
public class CrumbleWallOnRumble : TileEntity, ISolid, IPlaceable {
    public override int Depth => Bool("blendin") ? -10501 : -12999;

    public override char Tiletype => Char("tiletype", 'm');

    public override TileLayer Layer => TileLayer.FG;

    public static FieldList GetFields() => new(new {
        tiletype = Fields.TileDropdown('m', bg: false),
        blendin = true,
        permanent = false,
    });

    public static PlacementList GetPlacements() => new("crumble_wall");
}
