using Rysy.Graphics;

namespace Rysy.Helpers;

public static class NineSliceHelper
{
    public static IEnumerable<ISprite> GetSprites(Entity e, Func<NineSliceLocation, int, int, Sprite> spriteGetter)
    {
        var w = e.Width;
        var wBy8 = w / 8;
        var h = e.Height;
        var hBy8 = h / 8;

        for (int x = 0; x < wBy8; x++)
        {
            var left = x == 0;
            var middle = x + 1 < wBy8;
            yield return GetTexture(left ? NineSliceLocation.TopLeft : middle ? NineSliceLocation.TopMiddle : NineSliceLocation.TopRight,       x, 0, x * 8f, 0f);
            yield return GetTexture(left ? NineSliceLocation.BottomLeft : middle ? NineSliceLocation.BottomMiddle : NineSliceLocation.BottomRight, x, hBy8 - 1, x * 8f, h - 8f);
            for (int y = 1; y < hBy8 - 1; y++)
            {
                yield return GetTexture(left ? NineSliceLocation.Left : (middle ? NineSliceLocation.Middle : NineSliceLocation.Right), x, y, x*8f,y*8f);
            }
        }

        Sprite GetTexture(NineSliceLocation loc, int tx, int ty, float offX, float offY)
        {
            var s = spriteGetter(loc, tx, ty);
            return s with
            {
                Pos = e.Pos + s.Pos + new Vector2(offX, offY)
            };
        }
    }
}
