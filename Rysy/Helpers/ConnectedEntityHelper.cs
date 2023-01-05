using Rysy.Graphics;

namespace Rysy.Helpers;

public static class ConnectedEntityHelper {
    public delegate Sprite ConnectedEntityHelperSpriteGetter(NineSliceLocation loc);

    public static IEnumerable<ISprite> GetSprites(Entity self, IEnumerable<Entity> otherEntities, ConnectedEntityHelperSpriteGetter getTexture, bool ignoreMiddle = false, bool handleInnerCorners = false) {
        var w = self.Width;
        var h = self.Height;

        var others = otherEntities.ToList();

        // topmiddle and botmiddle
        for (int x = 8; x < w - 8; x += 8) {
            if (Open(self, others, x, -8f)) {
                yield return GetTexture(NineSliceLocation.TopMiddle, x, 0f);
            } else if (handleInnerCorners) {
                if (Open(self, others, x + 8, -8f)) {
                    yield return GetTexture(NineSliceLocation.InnerCorner_UpRight, x, 0f);
                } else if (Open(self, others, x - 8, -8f)) {
                    yield return GetTexture(NineSliceLocation.InnerCorner_UpLeft, x, 0f);
                } else
                    yield return GetTexture(NineSliceLocation.Middle, x, 0f);
            }


            if (Open(self, others, x, h)) {
                yield return GetTexture(NineSliceLocation.BottomMiddle, x, h - 8f);
            } else if (handleInnerCorners) {
                if (Open(self, others, x + 8, h)) {
                    yield return GetTexture(NineSliceLocation.InnerCorner_DownRight, x, h - 8f);
                } else if (Open(self, others, x - 8, h)) {
                    yield return GetTexture(NineSliceLocation.InnerCorner_DownLeft, x, h - 8f);
                } else
                    yield return GetTexture(NineSliceLocation.Middle, x, h - 8f);
            }
        }

        // leftmid, rightmid
        for (int y = 8; y < h - 8; y += 8) {
            if (Open(self, others, -8f, y)) {
                yield return GetTexture(NineSliceLocation.Left, 0f, y);
            } else if (handleInnerCorners) {
                if (Open(self, others, -8f, y - 8))
                    yield return GetTexture(NineSliceLocation.InnerCorner_UpLeft, 0f, y);
                else if (Open(self, others, -8f, y + 8))
                    yield return GetTexture(NineSliceLocation.InnerCorner_DownLeft, 0f, y);
                else
                    yield return GetTexture(NineSliceLocation.Middle, 0f, y);
            }


            if (Open(self, others, w, y)) {
                yield return GetTexture(NineSliceLocation.Right, w - 8f, y);
            } else if (handleInnerCorners) {
                if (Open(self, others, w + 8, y - 8))
                    yield return GetTexture(NineSliceLocation.InnerCorner_UpRight, w - 8f, y);
                else if (Open(self, others, w + 8, y + 8))
                    yield return GetTexture(NineSliceLocation.InnerCorner_DownRight, w - 8f, y);
                else
                    yield return GetTexture(NineSliceLocation.Middle, w - 8f, y);
            }
        }

        if (!ignoreMiddle) {
            for (int x = 8; x < w - 8; x++) {
                for (int y = 8; y < h - 8; y++) {
                    yield return GetTexture(NineSliceLocation.Middle, x, y);
                }
            }
        }

        // CORNERS

        // leftTop
        {
            var top = Open(self, others, 0, -8f);
            var left = Open(self, others, -8, 0f);

            yield return GetTexture((top, left) switch {
                (true, true) => NineSliceLocation.TopLeft,
                (true, false) => NineSliceLocation.TopMiddle,
                (false, true) => NineSliceLocation.Left,
                (false, false) => NineSliceLocation.InnerCorner_UpLeft, // todo - unchecked
            }, 0f, 0f);
        }

        // rightTop
        {
            var top = Open(self, others, w - 8, -8f);
            var right = Open(self, others, w, 0f);

            yield return GetTexture((top, right) switch {
                (true, true) => NineSliceLocation.TopRight,
                (true, false) => NineSliceLocation.TopMiddle,
                (false, true) => NineSliceLocation.Right,
                (false, false) => NineSliceLocation.InnerCorner_UpRight,
            }, w - 8f, 0f);
        }

        // leftBot
        {
            var bot = Open(self, others, 0, h + 8f);
            var left = Open(self, others, -8, h - 8);

            yield return GetTexture((bot, left) switch {
                (true, true) => NineSliceLocation.BottomLeft,
                (true, false) => NineSliceLocation.BottomMiddle,
                (false, true) => NineSliceLocation.Left,
                (false, false) => NineSliceLocation.InnerCorner_DownLeft,
            }, 0f, h - 8f);
        }

        // rightBot
        {
            var bot = Open(self, others, w - 8, h + 8f);
            var right = Open(self, others, w, h - 8);

            yield return GetTexture((bot, right) switch {
                (true, true) => NineSliceLocation.BottomRight,
                (true, false) => NineSliceLocation.BottomMiddle,
                (false, true) => NineSliceLocation.Right,
                (false, false) => NineSliceLocation.InnerCorner_DownRight, // todo - unchecked
            }, w - 8f, h - 8f);
        }


        Sprite GetTexture(NineSliceLocation loc, float offX, float offY) {
            var s = getTexture(loc);
            return s with {
                Pos = self.Pos + s.Pos + new Vector2(offX, offY)
            };
        }
    }

    private static bool Open(Entity self, List<Entity> blocks, float offX, float offY) {
        var selfRect = new Rectangle((int) (self.Pos.X + offX + 4f), (int) (self.Pos.Y + offY + 4f), 1, 1);

        foreach (var other in blocks) {
            if (other == self) {
                continue;
            }

            if (selfRect.Intersects(other.Rectangle)) {
                return false;
            }
        }

        return true;
    }
}
