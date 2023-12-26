using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("waterfall")]
public class Waterfall : Entity, IPlaceable {
    private static readonly SpriteTemplate FillSprite = SpriteTemplate.FromTexture("Rysy:waterfallFill", -9999);
    private static readonly SpriteTemplate OutlineSprite = SpriteTemplate.FromTexture("Rysy:waterfallOutline", -9999);
    
    public virtual Color SurfaceColor => Color.LightSkyBlue * 0.8f;
    public virtual Color FillColor => Color.LightSkyBlue * 0.3f;

    public override int Depth => -9999;

    public override IEnumerable<ISprite> GetSprites() {
        return GetSprites(Room, Pos, FillColor, SurfaceColor);
    }

    public static IEnumerable<ISprite> GetSprites(Room room, Vector2 pos, Color fillColor, Color surfaceColor) {
        var h = GetHeight(room, pos);
        pos = pos.AddX(-1);
        
        var th = FillSprite.Texture.Height;
        var tw = FillSprite.Texture.Width;
        
        for (int y = 0; y < h; y += th) {
            var innerPos = new Vector2(pos.X, pos.Y + y);
            if (y + th > h) {
                // Make sure we don't overshoot the height
                yield return FillSprite.CreateUntemplated(innerPos, fillColor).CreateSubtexture(0, 0, tw, h - y);
                yield return OutlineSprite.CreateUntemplated(innerPos, surfaceColor).CreateSubtexture(0, 0, tw, h - y);
                break;
            }
            
            yield return FillSprite.Create(innerPos, fillColor);
            yield return OutlineSprite.Create(innerPos, surfaceColor);
        }
    }

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.AddX(-1), 10, GetHeight(Room, Pos));

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
