using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class RectangleEntity : Entity
{
    public abstract Color Color { get; }
    public abstract Color OutlineColor { get; }

    public override IEnumerable<ISprite> GetSprites()
    {
        var w = Width switch
        {
            0 => 8,
            var other => other
        };
        var h = Height switch
        {
            0 => 8,
            var other => other
        };
        var rect = new Rectangle((int)Pos.X, (int)Pos.Y, w, h);

        yield return ISprite.HollowRect(rect, Color, OutlineColor);
    }
}
