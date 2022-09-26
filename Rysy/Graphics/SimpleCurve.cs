namespace Rysy.Graphics;

public record struct SimpleCurve
{
    public Vector2 Start, End, Control;

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
}
