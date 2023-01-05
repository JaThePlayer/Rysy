using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("water")]
public class Water : Entity, IWaterfallBlocker {
    public virtual Color SurfaceColor => Color.LightSkyBlue * 0.8f;
    public virtual Color FillColor => Color.LightSkyBlue * 0.3f;

    public virtual bool HasBottom => Bool("hasBottom", false);

    public override int Depth => -9999;

    public override IEnumerable<ISprite> GetSprites() {
        var w = Width;
        var h = Height;
        var surface = SurfaceColor;
        var hasBottom = HasBottom;

        // top surface
        yield return ISprite.Rect(Pos, w, 1, surface);

        // fill
        yield return ISprite.Rect(Pos + new Vector2(0f, -1f), w, hasBottom ? h - 1 : h, FillColor);

        if (hasBottom) {
            yield return ISprite.Rect(Pos + new Vector2(0f, h - 1), w, 1, surface);
        }
    }
}
