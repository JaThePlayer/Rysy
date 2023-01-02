using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cassette")]
public class Cassette : SpriteEntity
{
    public override string TexturePath => "collectables/cassette/idle00";

    public override int Depth => -1000000;
}
