using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("waterfall")]
public class Waterfall : Entity, IPlaceable {
    public virtual Color SurfaceColor => Color.LightSkyBlue * 0.8f;
    public virtual Color FillColor => Color.LightSkyBlue * 0.3f;

    public override int Depth => -9999;

    public override IEnumerable<ISprite> GetSprites() {
        var h = GetHeight(Room, Pos);

        yield return ISprite.OutlinedRect(Pos, 8, h, FillColor, SurfaceColor);
    }

    public static int GetHeight(Room room, Vector2 pos) {
        var h = 8;

        var maxH = room.Height - pos.Y;
        while (h < maxH && !room.IsTileAt(pos + new Vector2(0, h))) {
            h += 8;
        }

        var rect = new Rectangle((int) pos.X, (int) pos.Y, 8, h);

        foreach (var e in room.Entities[typeof(IWaterfallBlocker)]) {
            if (e is IWaterfallBlocker { BlockWaterfalls: true }) {
                Rectangle bRect = e.Rectangle;

                if (bRect.Intersects(rect)) {
                    h = (int) (e.Y - pos.Y);
                    rect.Height = h;
                }
            }
        }

        return h;
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("waterfall");
}
