using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("scale")]
public sealed class ScaleDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        multiplyX = 1f,
        multiplyY = 1f,
    });

    public static PlacementList GetPlacements() => new("default");
    
    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        yield return ISprite.FromTexture(texture).Centered() with {
            Scale = new(Data.Float("multiplyX", 1f), Data.Float("multiplyY", 1f))
        };
    }
}