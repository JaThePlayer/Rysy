using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("bloom")]
public sealed class BloomDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public override bool AllowMultiple => true;

    public static FieldList GetFields() => new(new {
        offsetX = 0f,
        offsetY = 0f,
        alpha = Fields.Float(1f).WithMin(0f).WithMax(1f),
        radius = Fields.Float(1f).WithMin(0f)
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        var gradientSprite = GFX.Atlas["util/bloomgradient"];
        var scale = Data.Float("radius", 1f) * 2f * (1f / gradientSprite.Width);
        var alpha = Data.Float("alpha", 1f).Div(2f).AtLeast(0f).AtMost(0.6f);
        var offset = new Vector2(Data.Float("offsetX"), Data.Float("offsetY"));
        
        yield return ISprite.FromTexture(offset, gradientSprite).Centered() with {
            Scale = new(scale),
            Color = Color.White * alpha
        };
        
        yield return ISprite.FromTexture(texture).Centered();
    }
}