using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("lamp")]
public sealed class Lamp : Entity, IPlaceable {
    public override int Depth => 5;

    public override IEnumerable<ISprite> GetSprites() {
        var lampTexture = GFX.Atlas["scenery/lamp"];

        var (w, h) = (lampTexture.Width, lampTexture.Height);

        yield return ISprite.FromTexture(Pos, lampTexture).CreateSubtexture(Bool("broken") ? w / 2 : 0, 0, w / 2, h) with {
            Origin = new(0.5f, 1f)
        };
    }

    public static FieldList GetFields() => new(new {
        broken = false
    });

    public static PlacementList GetPlacements() => new() {
        new("normal"),
        new("broken", new {
            broken = true
        })
    };
}