using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("door")]
public class Door : SpriteEntity
{
    public override string TexturePath => Attr("type", "wood") switch
    {
        "wood" => "objects/door/door00",
        "metal" or _ => "objects/door/metaldoor00",
    };

    public override Vector2 Origin => new(.5f, 1f);

    public override int Depth => 8998;
}
