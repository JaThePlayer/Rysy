using Rysy.Graphics;

namespace Rysy.Stylegrounds;

[CustomEntity("northernlights")]
public sealed class NorthernLights : Style, IPlaceable {
    // LinearGradientSprites are expensive to create, and GetSprites gets called each frame the styleground is visible.
    private LinearGradientSprite? _cachedSprite;
    private static LinearGradientSprite? _cachedPreviewSprite;

    public override IEnumerable<ISprite> GetPreviewSprites() {
        return _cachedPreviewSprite
            ??= ISprite.LinearGradient(new(0, 0, 320, 180), Gradient, LinearGradient.Directions.Vertical);
    }

    // Simplified rendering, which only renders the gradient
    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        var bounds = new Rectangle(0, 0, (int) (320 * 6f / ctx.Camera.Scale), (int) (180 * 6f / ctx.Camera.Scale));
        
        // if zoom level changed, clear the cache.
        if (_cachedSprite is { } && _cachedSprite.Bounds != bounds) {
            _cachedSprite = null;
        }
        
        return _cachedSprite ??= ISprite.LinearGradient(bounds, Gradient, LinearGradient.Directions.Vertical);
    }


    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");
    
    private static readonly LinearGradient Gradient = LinearGradient.Parse("020825,170c2f,100", null);
}