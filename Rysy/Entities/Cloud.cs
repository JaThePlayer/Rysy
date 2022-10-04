using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cloud")]
public class Cloud : SpriteEntity
{
    public override string TexturePath => (Bool("fragile", false), Bool("small", false)) switch
    {
        (true,  false) => "objects/clouds/fragile00",
        (true,  true)  => "objects/clouds/fragileRemix00",
        (false, false) => "objects/clouds/cloud00",
        (false, true)  => "objects/clouds/cloudRemix00",
    };

    public override Vector2 Origin => new(.5f, 0f);

    //public override Vector2 Offset => new(0f, 8f); // because clouds use hardcoded 8 as origin... check if this is equivalent?

    public override int Depth => 0;
}
