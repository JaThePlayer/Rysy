using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fakeWall")]
public class FakeWall : TileEntity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.Fg;

    public static FieldList GetFields() => new() {
        ["tiletype"] = Fields.TileDropdown('3', bg: false)
    };

    public static PlacementList GetPlacements() => new("fake_wall");
}
