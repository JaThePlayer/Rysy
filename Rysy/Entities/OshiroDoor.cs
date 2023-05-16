using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("oshirodoor")]
public sealed class OshiroDoor : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos.Add(16, 16), "objects/door/ghost_door00").Centered();

        yield return ISprite.FromTexture(Pos.Add(16, 16), "characters/oshiro/oshiro24").Centered();
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("oshiro_door");
}