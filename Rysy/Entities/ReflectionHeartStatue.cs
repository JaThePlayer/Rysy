using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("reflectionHeartStatue")]
public sealed class ReflectionHeartStatue : Entity, IPlaceable {
    public override int Depth => 8999;

    public override Range NodeLimits => 5..5;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "objects/reflectionHeart/statue") with {
            Origin = new(0.5f, 1f)
        };
    }

    private static Color[] Colors = new Color[] {
        new(240f / 255, 240f / 255, 240f / 255),
        new(145f / 255, 113f / 255, 242f / 255),
        new(10f / 255, 68f / 255, 224f / 255),
        new(179f / 255, 45f / 255, 0f / 255),
        new(145f / 255, 113f / 255, 242f / 255),
        new(255f / 255, 205f / 255, 55f / 255)
    };

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var pos = Nodes[nodeIndex].Pos;

        if (nodeIndex <= 3) {
            yield return ISprite.FromTexture(pos.Add(-32, -64), "objects/reflectionHeart/torch00");
            yield return ISprite.FromTexture(pos.AddY(28), $"objects/reflectionHeart/hint{nodeIndex:d2}") with {
                Origin = new(.5f, .5f)
            };
            yield break;
        }

        var gemTexture = ISprite.FromTexture(pos, "objects/reflectionHeart/gem");

        for (int i = 0; i < Colors.Length; i++) {
            var c = Colors[i];

            yield return gemTexture with {
                Color = c,
                Pos = pos.AddX((i - (Colors.Length - 1) / 2) * 24)
            };
        }
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("statue");
}