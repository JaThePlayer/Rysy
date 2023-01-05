using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lockBlock")]
public sealed class KeyDoor : SpriteEntity {
    public override int Depth => Depths.Solids;

    public override string TexturePath => Attr("sprite", "wood") switch {
        "temple_a" => "objects/door/lockdoorTempleA00",
        "temple_b" => "objects/door/lockdoorTempleB00",
        "moon" => "objects/door/moonDoor11",
        "wood" or _ => "objects/door/lockdoor00",
    };

    public override Vector2 Offset => new(16f);
}
