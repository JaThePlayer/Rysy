using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("water")]
public class Water : RectangleEntity, IWaterfallBlocker, IPlaceable {
    public override Color OutlineColor => Color.LightSkyBlue * 0.8f;
    public override Color FillColor => Color.LightSkyBlue * 0.3f;

    public virtual bool HasBottom => Bool("hasBottom", false);

    public override int Depth => -9999;

    public static FieldList GetFields() => new(new {
        hasBottom = false,
    });

    public static PlacementList GetPlacements() => new("water");

    public override IEnumerable<ISprite> GetSprites() {
        var w = Width;
        var h = Height;
        var surface = OutlineColor;
        var hasBottom = HasBottom;

        // top surface
        yield return ISprite.Rect(Pos, w, 1, surface);

        // fill
        yield return ISprite.Rect(Rectangle, FillColor);

        if (hasBottom) {
            yield return ISprite.Rect(Pos + new Vector2(0f, h - 1), w, 1, surface);
        }
    }
}
