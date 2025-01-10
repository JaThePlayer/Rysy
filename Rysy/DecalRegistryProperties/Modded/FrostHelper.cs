using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties.Modded;

[CustomEntity("frosthelper.rainbow")]
internal sealed class FrostHelperRainbowDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        var sprites = base.GetSprites(texture, ctx);

        foreach (var spr in sprites) {
            if (spr is Sprite sprite) {
                yield return sprite.MakeRainbow();
            } else {
                yield return spr;
            }
        }
    }
}
