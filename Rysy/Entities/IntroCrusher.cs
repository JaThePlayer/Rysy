using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("introCrusher")]
public sealed class IntroCrusher : TileEntity, IPlaceable {
    public override int Depth => 0;

    public override Range NodeLimits => 1..1;

    public override char Tiletype => Char("tiletype", '3');

    public override TileLayer Layer => TileLayer.FG;

    public static FieldList GetFields() => new(new {
        tiletype = Fields.TileDropdown('3', bg: false),
        flags = Fields.List("1,0b", Fields.String("f")),
    });

    public static PlacementList GetPlacements() => new("intro_crusher");
}