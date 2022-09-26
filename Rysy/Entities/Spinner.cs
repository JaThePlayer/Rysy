using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("spinner")]
public sealed class Spinner : Entity
{
    public override int Depth => -8500;

    private static string[] SpinnerColors = {
        "blue",
        "red",
        "purple",
        "core",
        "rainbow"
    };

    private static string ColorToTexturePath(string color) => color switch
    {
        "red"     or "Red"     => "danger/crystal/fg_red00",
        "purple"  or "Purple"  => "danger/crystal/fg_purple00",
        "rainbow" or "Rainbow" => "danger/crystal/fg_white00",
        _ => "danger/crystal/fg_blue00"
    };

    public override IEnumerable<ISprite> GetSprites()
    {
        if (Bool("dust"))
        {
            yield return ISprite.FromTexture(Pos, "Rysy:util/dustSpriteOutlines/base00").Centered() with
            {
                Color = Color.Red,
                Depth = -48,
            };
            yield return ISprite.FromTexture(Pos, "danger/dustcreature/base00").Centered();
            yield break;
        }

        var sprite = ISprite.FromTexture(Pos, ColorToTexturePath(Attr("color", "blue"))).Centered();
        yield return sprite;
        yield return sprite with
        {
            OutlineColor = Color.Black,
            Depth = Depth + 1,
        };
    }
}
