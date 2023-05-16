using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("badelineBoost")]
public sealed class BadelineBoost : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/badelineboost/idle00";

    public override int Depth => -1000000;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        lockCamera = true,
        canSkip = false,
        finalCh9Boost = false,
        finalCh9GoldenBoost = false,
        finalCh9Dialog = false
    });

    public static PlacementList GetPlacements() => new("boost");
}
