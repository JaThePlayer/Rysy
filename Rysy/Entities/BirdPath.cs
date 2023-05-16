using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("birdPath")]
public class BirdPath : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/bird/flyup00";

    public override int Depth => 0;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        only_once = false,
        onlyIfLeft = false,
        speedMult = 1.0
    });

    public static PlacementList GetPlacements() => new("bird_path");
}
