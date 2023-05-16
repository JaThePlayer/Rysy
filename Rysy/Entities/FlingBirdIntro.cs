using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("flingBirdIntro")]
public sealed class FlingBirdIntro : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/bird/Hover04";

    public override int Depth => 0;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        crashes = false
    });

    public static PlacementList GetPlacements() => new("fling_bird");
}

