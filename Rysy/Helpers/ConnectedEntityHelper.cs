using Rysy.Extensions;
using Rysy.Graphics;
using System.Runtime.InteropServices;

namespace Rysy.Helpers;

public static class ConnectedEntityHelper {
    public delegate Sprite ConnectedEntityHelperSpriteGetter(Vector2 pos, NineSliceLocation loc);
    public delegate NineSliceLocation MaskToLocation(bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight);

    public static IEnumerable<ISprite> GetSprites(Entity self, IEnumerable<Entity> allEntities, ConnectedEntityHelperSpriteGetter getTexture, Func<Entity, Rectangle>? getRectangle = null, MaskToLocation? maskToLocation = null, bool ignoreMiddle = false, bool handleInnerCorners = false) {
        getRectangle ??= (entity) => entity.Rectangle;
        maskToLocation ??= DefaultMaskToLocation;

        var selfRect = getRectangle(self);
        var w = selfRect.Width;
        var h = selfRect.Height;
        var selfPos = selfRect.Location.ToVector2();

        var bounds = selfRect.AddSize(16, 16).MovedBy(-8, -8);

        // only grab the entities we have a chance of connecting to
        var others = allEntities.Append(self).Select(e => getRectangle(e)).Where(bounds.Intersects).ToList();

        Sprite spr;

        if (ignoreMiddle) {
            if (GetSprite(selfPos, others, getTexture, 0, 0, ignoreMiddle, handleInnerCorners, maskToLocation, out spr)) 
                yield return spr;

            if (GetSprite(selfPos, others, getTexture, w - 8, h - 8, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                yield return spr;

            if (GetSprite(selfPos, others, getTexture, w - 8, 0, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                yield return spr;

            if (GetSprite(selfPos, others, getTexture, 0, h - 8, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                yield return spr;

            for (int x = 8; x < w - 8; x += 8) {
                if (GetSprite(selfPos, others, getTexture, x, 0, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                    yield return spr;

                if (GetSprite(selfPos, others, getTexture, x, h - 8, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                    yield return spr;
            }

            for (int y = 8; y < h - 8; y += 8) {
                if (GetSprite(selfPos, others, getTexture, 0, y, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                    yield return spr;

                if (GetSprite(selfPos, others, getTexture, w - 8, y, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                    yield return spr;
            }

            yield break;
        }

        for (int x = 0; x < w; x += 8) {
            for (int y = 0; y < h; y += 8) {
                if (GetSprite(selfPos, others, getTexture, x, y, ignoreMiddle, handleInnerCorners, maskToLocation, out spr))
                    yield return spr;
            }
        }
    }

    public static (bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight) GetOpen(Vector2 pos, List<Rectangle> allRectangles, float x, float y) {
        var others = CollectionsMarshal.AsSpan(allRectangles);

        var (upLeft, up, upRight) = (Open(pos, others, x - 8, y - 8), Open(pos, others, x, y - 8), Open(pos, others, x + 8, y - 8));
        var (left, mid, right) = (Open(pos, others, x - 8, y), Open(pos, others, x, y), Open(pos, others, x + 8, y));
        var (botLeft, botMid, botRight) = (Open(pos, others, x - 8, y + 8), Open(pos, others, x, y + 8), Open(pos, others, x + 8, y + 8));

        return (upLeft, up, upRight, left, mid, right, botLeft, botMid, botRight);
    }

    private static bool GetSprite(Vector2 pos, List<Rectangle> allEntities, ConnectedEntityHelperSpriteGetter getTexture, float x, float y, bool ignoreMiddle, bool handleInnerCorners, MaskToLocation maskToLocation, out Sprite spr) {
        var others = CollectionsMarshal.AsSpan(allEntities);

        (bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight) = GetOpen(pos, allEntities, x, y);

        NineSliceLocation loc = maskToLocation(upLeft, up, upRight, left, mid, right, botLeft, botMid, botRight);

        if (ignoreMiddle && loc == NineSliceLocation.Middle) {
            spr = default;
            return false;
        }

        if (!handleInnerCorners && loc is NineSliceLocation.InnerCorner_UpRight or NineSliceLocation.InnerCorner_UpLeft or NineSliceLocation.InnerCorner_DownLeft or NineSliceLocation.InnerCorner_DownRight) {
            spr = default;
            return false;
        }

        spr = GetTexture(pos, loc, getTexture, x, y);
        return true;
    }

    private static NineSliceLocation DefaultMaskToLocation(bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight) {
        return (upLeft, up, upRight, left, mid, right, botLeft, botMid, botRight) switch {
            (true, false, _, false, _, _, _, _, _) => NineSliceLocation.InnerCorner_UpLeft,
            (_, false, true, _, _, false, _, _, _) => NineSliceLocation.InnerCorner_UpRight,

            (_, _, _, false, _, _, true, false, _) => NineSliceLocation.InnerCorner_DownLeft,
            (_, _, _, _, _, false, _, false, true) => NineSliceLocation.InnerCorner_DownRight,

            (_, true, _, true, _, false, _, _, _) => NineSliceLocation.TopLeft,
            (_, true, _, false, _, true, _, _, _) => NineSliceLocation.TopRight,
            (_, true, _, false, _, false, _, _, _) => NineSliceLocation.TopMiddle,

            (_, _, _, true, _, false, _, true, _) => NineSliceLocation.BottomLeft,
            (_, _, _, false, _, true, _, true, _) => NineSliceLocation.BottomRight,
            (_, _, _, false, _, false, _, true, _) => NineSliceLocation.BottomMiddle,

            (_, _, _, true, _, _, _, _, _) => NineSliceLocation.Left,
            (_, _, _, _, _, true, _, _, _) => NineSliceLocation.Right,

            _ => NineSliceLocation.Middle,
        };
    }

    static Sprite GetTexture(Vector2 pos, NineSliceLocation loc, ConnectedEntityHelperSpriteGetter getTexture, float offX, float offY) {
        return getTexture(pos + new Vector2(offX, offY), loc);
    }

    private static bool Open(Vector2 pos, Span<Rectangle> blocks, float offX, float offY) {
        var selfRect = new Rectangle((int) (pos.X + offX + 4f), (int) (pos.Y + offY + 4f), 1, 1);

        foreach (var other in blocks) {
            if (selfRect.Intersects(other)) {
                return false;
            }
        }

        return true;
    }
}
