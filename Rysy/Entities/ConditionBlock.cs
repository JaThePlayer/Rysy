using Rysy.Helpers;
using ConditionBlockModes = Rysy.Helpers.CelesteEnums.ConditionBlockModes;

namespace Rysy.Entities;

[CustomEntity("conditionBlock")]
internal sealed class ConditionBlock : TileEntity, IPlaceable {
    public override char Tiletype => Char("tileType", '3');

    public override TileLayer Layer => TileLayer.FG;

    public override int Depth => -13000;

    public static FieldList GetFields() => new(new {
        tileType = Fields.TileDropdown('3', bg: false),
        condition = ConditionBlockModes.Key,
        conditionID = "1:1",
    });

    public static PlacementList GetPlacements() => new("condition_block");
}
