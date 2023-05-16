using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cassette")]
public class Cassette : SpriteEntity, IPlaceable {
    public override string TexturePath => "collectables/cassette/idle00";

    public override int Depth => -1000000;

    public override Range NodeLimits => 2..2;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("cassette");
}
