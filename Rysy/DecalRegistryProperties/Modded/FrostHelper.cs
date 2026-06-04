using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties.Modded;

[CustomEntity("frosthelper.rainbow")]
internal sealed class FrostHelperRainbowDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    internal enum BlendingMode {
        /// <summary>
        /// The source color is ignored, the next color is used verbatim.
        /// </summary>
        IgnoreSource,
        /// <summary>
        /// The next color is used, but the alpha channel is set to the alpha value of the source color.
        /// </summary>
        CopySourceAlpha,
        /// <summary>
        /// Treats source and next as a straight alpha color (like decal tints), performs an alpha blend.
        /// </summary>
        AlphaBlend,
        /// <summary>
        /// The next color is added onto the original.
        /// </summary>
        Additive,
    }
    
    public static FieldList GetFields() => new(new {
        decalColorBlending = BlendingMode.IgnoreSource,
        alpha = Fields.Float(1.0f).WithMin(0f).WithMax(1f)
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
