using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("exitBlock")]
public sealed class ExitBlock : TileEntity, IPlaceable {
    public override char Tiletype => Char("tileType", '3');

    public override TileLayer Layer => TileLayer.Fg;

    public override int Depth => -13000;

    public static FieldList GetFields() => new(new {
        tileType = Fields.TileDropdown('3', bg: false),
        playTransitionReveal = false,
    });

    public static PlacementList GetPlacements() => new("exit_block");
}
