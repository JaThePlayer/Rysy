using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("seeker")]
public class Seeker : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/monsters/predator00";

    public override int Depth => -200;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("seeker");
}
