using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("flutterbird")]
public class Flutterbird : SpriteEntity, IPlaceable {
    private static readonly Color[] Colors = {
        "89FBFF".FromRgb(),
        "F0FC6C".FromRgb(),
        "F493FF".FromRgb(),
        "93BAFF".FromRgb()
    };

    public override int Depth => -9999;

    public override string TexturePath => "scenery/flutterbird/idle00";

    public override Color Color => Colors[Pos.SeededRandomExclusive(Colors.Length)];

    public override Vector2 Origin => new(0.5f, 1.0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("flutterbird");
}
