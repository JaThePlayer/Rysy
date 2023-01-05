using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("seeker")]
public class Seeker : SpriteEntity {
    public override string TexturePath => "characters/monsters/predator00";

    public override int Depth => -200;
}
