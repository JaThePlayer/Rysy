using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bigSpinner")]
public class Bumper : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/Bumper/Idle22";

    public override int Depth => 0;

    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("bumper");
}
