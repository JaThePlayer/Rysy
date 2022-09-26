using Rysy.Graphics;

namespace Rysy.Entities;

/// <summary>
/// Provides a base implementation for entities Rysy doesn't know about
/// </summary>
public sealed class UnknownEntity : Entity
{
    public static Color Color = Color.Green * .3f;
    public static Color OutlineColor = Color.Green;

    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites()
    {
        var w = Width;
        var h = Height;
        if (w != 0 || h != 0)
        {
            yield return ISprite.HollowRect(Pos, w == 0 ? 8 : w, h == 0 ? 8 : h, Color, OutlineColor);
        }
        else
        {
            yield return ISprite.HollowRect(Pos - new Vector2(2, 2), 4, 4, Color, OutlineColor);
        }
    }
}