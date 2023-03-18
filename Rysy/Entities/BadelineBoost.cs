using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("badelineBoost")]
public class BadelineBoost : SpriteEntity {
    public override string TexturePath => "objects/badelineboost/idle00";

    public override int Depth => -1000000;

    public override Range NodeLimits => 0..;
}
