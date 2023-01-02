using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("torch")]
public class Torch : SpriteEntity
{
    public override string TexturePath => Bool("startLit", false) ? "objects/temple/litTorch03" : "objects/temple/torch00";

    public override int Depth => 2000;
}
