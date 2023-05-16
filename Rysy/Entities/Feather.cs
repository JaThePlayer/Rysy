using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("infiniteStar")]
public sealed class Feather : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/flyFeather/idle00";

    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return GetSprite();

        if (Bool("shielded", false))
            yield return ISprite.Circle(Pos, 10f, Color.White, 3);
    }

    public static FieldList GetFields() => new(new {
        shielded = false,
        singleUse = false
    });

    public static PlacementList GetPlacements() => new("normal");
}
