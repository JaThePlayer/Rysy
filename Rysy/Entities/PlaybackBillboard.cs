using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("playbackBillboard")]
public sealed class PlaybackBillboard : Entity, IPlaceable {
    public override int Depth => Depths.BGDecals;

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    private Sprite GetSprite(Vector2 pos, NineSliceLocation loc) {
        var (x, y) = loc switch {
            NineSliceLocation.TopLeft => (0, 0),
            NineSliceLocation.TopMiddle => (8, 0),
            NineSliceLocation.TopRight => (16, 0),
            NineSliceLocation.Left => (0, 8),
            NineSliceLocation.Middle => (8, 8),
            NineSliceLocation.Right => (16, 8),
            NineSliceLocation.BottomLeft => (0, 16),
            NineSliceLocation.BottomMiddle => (8, 16),
            NineSliceLocation.BottomRight => (16, 16),
            NineSliceLocation.InnerCorner_UpRight => (32, 0),
            NineSliceLocation.InnerCorner_UpLeft => (24, 0),
            NineSliceLocation.InnerCorner_DownRight => (32, 16),
            NineSliceLocation.InnerCorner_DownLeft => (24, 16),
            _ => (0, 0)
        };

        return ISprite.FromTexture(pos, "scenery/tvSlices").CreateSubtexture(x, y, 8, 8);
    }

    private static NineSliceLocation MaskToLocation(bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight) {
        return (upLeft, up, upRight, left, mid, right, botLeft, botMid, botRight) switch {
            (_, _, _, _, _, true, _, true, false) => NineSliceLocation.TopLeft,
            (_, _, _, true, _, _, false, true, _) => NineSliceLocation.TopRight,
            (_, true, false, _, _, true, _, _, _) => NineSliceLocation.BottomLeft,
            (false, true, _, true, _, _, _, _, _) => NineSliceLocation.BottomRight,
            (_, _, _, _, _, false, _, false, _) => NineSliceLocation.InnerCorner_UpLeft,
            (_, _, _, false, _, _, _, false, _) => NineSliceLocation.InnerCorner_UpRight,
            (_, false, _, _, _, false, _, _, _) => NineSliceLocation.InnerCorner_DownLeft,
            (_, false, _, false, _, _, _, _, _) => NineSliceLocation.InnerCorner_DownRight,

            (_, _, _, _, _, _, _, false, _) => NineSliceLocation.TopMiddle,
            (_, _, _, _, _, false, _, _, _) => NineSliceLocation.Left,
            (_, _, _, false, _, _, _, _, _) => NineSliceLocation.Right,
            (_, false, _, _, _, _, _, _, _) => NineSliceLocation.BottomMiddle,
            _ => NineSliceLocation.Middle,
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        var rects = Room.Entities[typeof(PlaybackBillboard)].Select(e => e.Rectangle).ToList();

        int w = (int) (Width / 8f);
        int h = (int) (Height / 8f);
        Sprite spr;
        var pos = Pos;

        for (int x = -1; x <= w; x++) {
            if (AutoTile(x, -1, out spr))
                yield return spr;
            if (AutoTile(x, h, out spr))
                yield return spr;
        }
        for (int y = 0; y < h; y++) {
            if (AutoTile(-1, y, out spr))
                yield return spr;
            if (AutoTile(w, y, out spr))
                yield return spr;
        }


        var rect = Rectangle;
        yield return ISprite.Rect(rect, Color.Lerp(Color.DarkSlateBlue, Color.Black, 0.6f));

        var noise = GFX.Atlas["util/noise"];
        var nw = noise.Width;
        var nh = noise.Height;

        var (subPosX, subPosY) = (pos.SeededRandomInclusive(0, nw / 2), pos.Add(0, 1).SeededRandomInclusive(0, nh / 2));

        for (int x = 0; x < rect.Width; x += nw / 2) {
            var subWidth = Math.Min(rect.Width - x, nw / 2);

            for (int y = 0; y < rect.Height; y += nh / 2) {
                var subHeight = Math.Min(rect.Height - y, nh / 2);

                yield return ISprite.FromTexture(pos.Add(x, y), noise).CreateSubtexture(subPosX, subPosY, subWidth, subHeight) with {
                    Color = Color.White * 0.1f
                };
            }
        }


        bool AutoTile(int x, int y, out Sprite spr) {
            (bool upLeft, bool up, bool upRight, bool left, bool mid, bool right, bool botLeft, bool botMid, bool botRight)
                = ConnectedEntityHelper.GetOpen(pos, rects, x * 8, y * 8);
            if (!mid) {
                spr = default;
                return false;
            }

            var loc = MaskToLocation(upLeft, up, upRight, left, mid, right, botLeft, botMid, botRight);

            spr = GetSprite(pos.Add(x * 8, y * 8), loc);

            return true;
        }
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("playback_billboard");
}