using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("key")]
public sealed class Key : SpriteEntity
{
    public override string TexturePath => "collectables/key/idle00";

    public override int Depth => 0;
}
