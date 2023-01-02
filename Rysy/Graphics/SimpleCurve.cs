namespace Rysy.Graphics;

public record struct SimpleCurve
{
    public Vector2 Start, End, Control;

    public SimpleCurve()
    {

    }

    public SimpleCurve(Vector2 start, Vector2 end, Vector2 control)
    {
        Start = start;
        End = end;
        Control = control;
    }

    public Vector2 GetPointAt(float percent)
    {
        var reverse = 1f - percent;

        return reverse*reverse * Start + 2f*reverse*percent*Control + percent*percent*End;
    }

    public IEnumerable<ISprite> GetSprites(Color color, int resolution)
    {
        var start = Start;

        for (int i = 1; i <= resolution; i++)
        {
            var end = GetPointAt(i / (float)resolution);

            yield return ISprite.Line(start, end, color);

            start = end;
        }
    }

    public IEnumerable<ISprite> GetSpritesForFloatyRectangle(Rectangle baseRect, Color color)
    {
        var clothStart = baseRect.Location.ToVector2();
        var len = baseRect.Width;
        var height = baseRect.Height;
        for (float i = 1f; i <= len; i++)
        {
            var clothSliceEnd = GetPointAt(i / len);
            clothSliceEnd.Floor();
            if (clothStart.X < clothSliceEnd.X)
            {
                yield return ISprite.Rect(clothStart, (int)(clothSliceEnd.X - clothStart.X + 1f), height, color);
                clothStart = clothSliceEnd;
            }
        }
    }

    /// <summary>
    /// Causes preloading of <paramref name="spr"/>'s texture!
    /// </summary>
    public IEnumerable<ISprite> GetSpritesForFloatySprite(Sprite spr)
    {
        var clothStart = Start;
        var len = spr.ForceGetWidth();
        var height = spr.ForceGetHeight();
        
        for (float i = 1f; i <= len; i++)
        {
            var p = i / len;
            var clothSliceEnd = GetPointAt(p);
            clothSliceEnd.Floor();
            if (clothStart.X < clothSliceEnd.X)
            {
                var w = (int)(clothSliceEnd.X - clothStart.X + 2f);
                if (w + i >= len)
                {
                    break;
                }
                yield return spr.CreateSubtexture((int)i,0, w, height) with
                {
                    Pos = clothStart
                };
                clothStart = clothSliceEnd;
            }
        }
    }
}
