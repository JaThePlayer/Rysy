using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fallingBlock")]
public class FallingBlock : TileEntity, ISolid, IPlaceable {
    public override int Depth => Bool("behind") ? 5000 : 0;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.FG;

    public override Color Color => Color.White;

    public static FieldList GetFields() => new(new {
        tiletype = Fields.TileDropdown('3', bg: false),
        climbFall = true,
        behind = false,
    });

    public static PlacementList GetPlacements() => new("falling_block");
}

