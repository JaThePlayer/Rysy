using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("key")]
public sealed class Key : SpriteEntity {
    public override string TexturePath => "collectables/key/idle00";

    public override int Depth => 0;

    public override Range NodeLimits => Nodes is { Count: > 0 } ? 2..2 : 0..0;
}
