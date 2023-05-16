using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

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

    private static string ColorToConnectorPath(string color) => color switch {
        "purple" or "Purple" => "danger/crystal/bg_purple00",
        "rainbow" or "Rainbow" => "danger/crystal/bg_white00",
        "blue" or "Blue" => "danger/crystal/bg_blue00",
        _ => "danger/crystal/bg_red00",
    };

    public bool AttachToSolid => Bool("attachToSolid");
    public bool Dust => Bool("dust");

    public override IEnumerable<ISprite> GetSprites() {
        if (Dust) {
            yield return ISprite.FromTexture(Pos, "Rysy:dust_creature_outlines/base00").Centered() with {
                Color = Color.Red,
                Depth = -48,
            };
            yield return ISprite.FromTexture(Pos, "danger/dustcreature/base00").Centered();
            yield break;
        }

        var color = Attr("color", "blue");
        var rainbow = color.Equals("rainbow", StringComparison.OrdinalIgnoreCase);
        var pos = Pos;

        var sprite = ISprite.FromTexture(pos, ColorToTexturePath(color)).Centered();
        if (rainbow)
            sprite.Color = ColorHelper.GetRainbowColor(Room, pos);

        yield return sprite;
        // the border has to be a seperate sprite to render it at a different depth
        yield return sprite with {
            Color = Color.Transparent,
            OutlineColor = Color.Black,
            Depth = Depth + 2,
        };

        // connectors
        var attachToSolid = AttachToSolid;
        var baseConnectorSprite = ISprite.FromTexture(ColorToConnectorPath(color)) with {
            Depth = Depth + 1,
            Origin = new(.5f, .5f),
        };

        foreach (Spinner spinner in Room.Entities[typeof(Spinner)]) {
            if (spinner == this)
                break;

            var otherPos = spinner.Pos;
            if (Vector2.DistanceSquared(pos, otherPos) < 24f * 24f && !spinner.Dust && spinner.AttachToSolid == attachToSolid) {
                var connectorPos = (pos + otherPos) / 2f;

                yield return baseConnectorSprite with {
                    Pos = connectorPos,
                    Color = rainbow ? ColorHelper.GetRainbowColor(Room, connectorPos) : Color.White,
                };

                // the border has to be a seperate sprite to render it at a different depth
                yield return baseConnectorSprite with {
                    Pos = connectorPos,
                    OutlineColor = Color.Black,
                    Color = Color.Transparent,
                    Depth = Depth + 2,
                };
            }
        }
    }

    public static FieldList GetFields() => new() {
        ["color"] = Fields.Dropdown("blue", SpinnerColors),
        ["attachToSolid"] = Fields.Bool(false),
        ["dust"] = Fields.Bool(false),
    };

    public static PlacementList GetPlacements() => new() {
        new("Spinner")
    };
}
