using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("kevins_pc")]
public sealed class KevinsPc : Entity, IPlaceable {
    public override int Depth => 8999;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos, "objects/kevinspc/pc") with {
            Origin = new(0.5f, 1f)
        };
        yield return ISprite.FromTexture(pos.Add(-16, -39), "objects/kevinspc/spectogram").CreateSubtexture(0, 0, 32, 18);
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("kevins_pc");
}