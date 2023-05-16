using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bigWaterfall")]
public class BigWaterfall : RectangleEntity, IPlaceable {
    public override Color OutlineColor => Color.LightSkyBlue * 0.8f;
    public override Color FillColor => Color.LightSkyBlue * 0.3f;

    public override int Depth => Enum("layer", Layers.BG) switch {
        Layers.FG => -49900,
        Layers.BG or _ => 10010,
    };

    public static FieldList GetFields() => new(new {
        layer = Fields.EnumNamesDropdown(Layers.FG)
    });

    public static PlacementList GetPlacements() => new() {
        new("foreground"),
        new("background", new {
            layer = Layers.BG
        }),
    };

    public override IEnumerable<ISprite> GetSprites() {
        var width = Width;
        var fillColor = FillColor;
        var outlineColor = OutlineColor;

        for (int y = 0; y < Height; y++) {
            var waveLeft = (int) float.Round(MathF.Sin(y / 8f).Abs() * 2) + 2;
            var waveRight = (int) float.Round(MathF.Sin((y / 8f) + MathHelper.PiOver2).Abs() * 2) + 2;

            yield return ISprite.Rect(Pos + new Vector2(waveLeft, y), width - waveLeft - waveRight, 1, fillColor);

            yield return ISprite.Rect(Pos + new Vector2(0, y), waveLeft, 1, outlineColor);
            yield return ISprite.Rect(Pos + new Vector2(waveLeft + 1, y), 1, 1, outlineColor);

            yield return ISprite.Rect(Pos + new Vector2(width - waveRight - 2, y), 1, 1, outlineColor);
            yield return ISprite.Rect(Pos + new Vector2(width - waveRight, y), waveRight, 1, outlineColor);
        }
    }


    public enum Layers {
        FG,
        BG
    }
}
