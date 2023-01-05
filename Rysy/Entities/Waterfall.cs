using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("waterfall")]
public class Waterfall : Entity {
    public virtual Color SurfaceColor => Color.LightSkyBlue * 0.8f;
    public virtual Color FillColor => Color.LightSkyBlue * 0.3f;

    public override int Depth => -9999;

    public override IEnumerable<ISprite> GetSprites() {
        var h = GetHeight();

        yield return ISprite.HollowRect(Pos, 8, h, FillColor, SurfaceColor);
    }

    public int GetHeight() {
        var room = Room;
        var h = 8;

        var maxH = room.Height - Pos.Y;
        while (h < maxH && !room.IsTileAt(Pos + new Vector2(0, h))) {
            h += 8;
        }

        var rect = new Rectangle((int) Pos.X, (int) Pos.Y, 8, h);

        foreach (var e in room.Entities[typeof(IWaterfallBlocker)]) {
            if (e is IWaterfallBlocker { BlockWaterfalls: true }) {
                Rectangle bRect = e.Rectangle;

                if (bRect.Intersects(rect)) {
                    h = (int) (e.Pos.Y - Pos.Y);
                    rect.Height = h;
                }
            }
        }

        return h;
    }
}
