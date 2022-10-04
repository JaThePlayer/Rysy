using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cliffside_flag")]
public class CliffsideWindFlag : SpriteEntity
{
    public override string TexturePath => $"scenery/cliffside/flag{Int("index", 0):d2}";

    public override Vector2 Origin => new();

    public override int Depth => 8999;
}
