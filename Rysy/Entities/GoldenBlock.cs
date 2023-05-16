using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("goldenBlock")]
public sealed class GoldenBlock : NineSliceEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "objects/goldblock";

    public override string? CenterSpritePath => "collectables/goldberry/idle00";

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("golden_block");
}