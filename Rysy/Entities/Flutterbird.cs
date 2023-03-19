using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("flutterbird")]
public class Flutterbird : SpriteEntity {
    private static readonly Color[] Colors = {
        "89FBFF".FromRGB(),
        "F0FC6C".FromRGB(),
        "F493FF".FromRGB(),
        "93BAFF".FromRGB()
    };

    public override int Depth => -9999;

    public override string TexturePath => "scenery/flutterbird/idle00";

    public override Color Color => Colors[Pos.SeededRandomExclusive(Colors.Length)];

    public override Vector2 Origin => new(0.5f, 1.0f);
}
