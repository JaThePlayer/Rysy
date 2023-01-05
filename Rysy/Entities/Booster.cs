using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("booster")]
public class Booster : SpriteEntity {
    public override int Depth => -8500;
    public override string TexturePath => Bool("red", false) ? "objects/booster/boosterRed00" : "objects/booster/booster00";
}
