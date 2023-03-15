using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("spinner")]
public sealed class Spinner : Entity, IPlaceable {
    public override int Depth => -8500;

    private static Dictionary<string, string> SpinnerColors = new(StringComparer.OrdinalIgnoreCase) {
        ["blue"] = "Blue",
        ["red"] = "Red",
        ["purple"] = "Purple",
        ["core"] = "Core",
        ["rainbow"] = "Rainbow"
    };

    private static string ColorToTexturePath(string color) => color switch {
        "purple" or "Purple" => "danger/crystal/fg_purple00",
        "rainbow" or "Rainbow" => "danger/crystal/fg_white00",
        "blue" or "Blue" => "danger/crystal/fg_blue00",
        _ => "danger/crystal/fg_red00",
    };

    public override IEnumerable<ISprite> GetSprites() {
        if (Bool("dust")) {
            yield return ISprite.FromTexture(Pos, "Rysy:dust_creature_outlines/base00").Centered() with {
                Color = Color.Red,
                Depth = -48,
            };
            yield return ISprite.FromTexture(Pos, "danger/dustcreature/base00").Centered();
            yield break;
        }

        var sprite = ISprite.FromTexture(Pos, ColorToTexturePath(Attr("color", "blue"))).Centered();
        yield return sprite;
        yield return sprite with {
            OutlineColor = Color.Black,
            Depth = Depth + 1,
        };
    }

    public static FieldList GetFields() => new() {
        ["color"] = Fields.Dropdown("blue", SpinnerColors),
        ["attachToSolid"] = Fields.Bool(false),
        ["dust"] = Fields.Bool(false),
    };

    public static List<Placement>? GetPlacements() => new() {
        new("Spinner")
    };
}
