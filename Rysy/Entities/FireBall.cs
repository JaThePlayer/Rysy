using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fireBall")]
public class FireBall : SpriteEntity {
    public override string TexturePath => Bool("notCoreMode", false) ? "objects/fireball/fireball09" : "objects/fireball/fireball01";

    public override int Depth => 0;

    public override Range NodeLimits => 0..;
}
