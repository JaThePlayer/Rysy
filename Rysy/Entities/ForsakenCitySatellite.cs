using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("birdForsakenCityGem")]
public sealed class ForsakenCitySatellite : Entity, IPlaceable {
    private static Dictionary<string, (float Angle, Color)> BirdColors => new() {
        ["U"] = (-90f.ToRad(), "f0f0f0".FromRGB()),
        ["L"] = (-180f.ToRad(), "9171f2".FromRGB()),
        ["DR"] = (45f.ToRad(), "0a44e0".FromRGB()),
        ["UR"] = (-45f.ToRad(), "b32d00".FromRGB()),
        ["UL"] = (-135f.ToRad(), "ffcd37".FromRGB())
    };

    public override int Depth => 8999;

    public override Range NodeLimits => 2..2;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "objects/citysatellite/dish") with {
            Origin = new(0.5f, 1.0f),
        };

        yield return ISprite.FromTexture(Pos, "objects/citysatellite/light") with {
            Origin = new(0.5f, 1.0f),
        };

        yield return ISprite.FromTexture(Pos + new Vector2(8, 8), "objects/citysatellite/computer");
        yield return ISprite.FromTexture(Pos + new Vector2(8, 8), "objects/citysatellite/computerscreen");
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var node = Nodes![nodeIndex];

        switch (nodeIndex) {
            case 0: {
                // birds
                var circle = ISprite.Circle(node, 12f, default, default);

                // return the circle to make selections nice
                yield return circle;

                foreach (var (_, (angle, color)) in BirdColors) {
                    yield return ISprite.FromTexture(circle.PointAtAngle(angle), "scenery/flutterbird/flap01").Centered() with {
                        Color = color,
                    };
                }

                break;
            }
            case 1:
                yield return ISprite.FromTexture(node, "collectables/heartGem/0/00").Centered();
                break;
        }

        yield break;
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("satellite");
}
