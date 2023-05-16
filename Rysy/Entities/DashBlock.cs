using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("dashBlock")]
public class DashBlock : TileEntity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.FG;

    public static FieldList GetFields() => new(new {
        tiletype = Fields.TileDropdown('m', bg: false),
        blendin = true,
        canDash = true,
        permanent = false,
    });

    public static PlacementList GetPlacements() => new("dash_block");
}
