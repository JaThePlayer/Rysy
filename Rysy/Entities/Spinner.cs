using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("spinner")]
public class Spinner : Entity
{
    private static readonly string DefaultSpinnerColor = "blue";
    private static readonly string UnknownSpinnerColor = "blue";
    private static readonly string[] SpinnerColors = {
        "blue",
        "red",
        "purple",
        "core",
        "rainbow"
    };

    private static readonly Dictionary<string, string> CustomSpinnerColors = new Dictionary<string, string>()
    {
        ["core"] = "red",
        ["rainbow"] = "white"
    };

    public override IEnumerable<ISprite> GetSprites()
    {
        var color = string.IsNullOrEmpty(Attr("color")) ? DefaultSpinnerColor : Attr("color");
        color = color.ToLowerInvariant();

        var dusty = Bool("dust");

        if (dusty)
        {
            var textureBase = "danger/dustcreature/base00";
            var textureCenter = "danger/dustcreature/center00";

            yield return ISprite.FromTexture(Pos, textureBase).Centered();
            yield return ISprite.FromTexture(Pos, textureCenter).Centered();
        }
        else
        {
            if (CustomSpinnerColors.ContainsKey(color))
            {
                color = CustomSpinnerColors[color];
            }

            var texture = $"danger/crystal/fg_{color}00";
            var sprite = ISprite.FromTexture(Pos, texture);

            if (sprite != null)
            {
                yield return sprite.Centered();
            }
            else
            {
                texture = $"danger/crystal/fg_{UnknownSpinnerColor}00";

                yield return ISprite.FromTexture(Pos, texture).Centered();
            }
        }
    }

    public override int Depth => Bool("dust") ? -50 : -8500;
}

/*
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
        "purple"  or "Purple"  => "danger/crystal/fg_purple00",
        "rainbow" or "Rainbow" => "danger/crystal/fg_white00",
        "blue"    or "Blue"    => "danger/crystal/fg_blue00",
        _ => "danger/crystal/fg_red00",
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
*/