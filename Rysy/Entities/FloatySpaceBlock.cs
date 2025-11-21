using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("floatySpaceBlock")]
public sealed class FloatySpaceBlock : TileEntity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.Fg;

    public static FieldList GetFields() => new(new {
        tiletype = Fields.TileDropdown('3', bg: false),
        disableSpawnOffset = false,
    });

    public static PlacementList GetPlacements() => [
        new Placement("floaty_space_block") {
            AlternativeNames = [ "moon_block" ]
        }
    ];

    public override bool CanTrim(string key, object val) => IsDefault(key, val);
}
