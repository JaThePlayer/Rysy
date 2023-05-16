using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("introCar")]
public sealed class IntroCar : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos, "scenery/car/body") with {
            Depth = 1,
            Origin = new(0.5f, 1f)
        };

        yield return ISprite.FromTexture(pos, "scenery/car/wheels") with {
            Depth = 3,
            Origin = new(0.5f, 1f)
        };

        if (Bool("hasRoadAndBarriers")) {
            var pavementWidth = pos.X - 48;
            var columns = pavementWidth / 8;

            var pavementSprite = ISprite.FromTexture(pos, "scenery/car/pavement") with {
                Depth = -10001,
            };

            for (int i = 0; i < columns; i++) {
                var offset = new Vector2(i * 8 - pos.X, 0);

                var idx = i == columns - 2 ? 2
                    : i > columns - 2 ? 3
                    : offset.SeededRandomInclusive(0, 2);

                yield return pavementSprite.MovedBy(offset).CreateSubtexture(idx * 8, 0, 8, 8);
            }

            yield return ISprite.FromTexture(pos.AddX(32), "scenery/car/barrier") with {
                Origin = new(0.0f, 1.0f),
                Depth = -10,
            };

            yield return ISprite.FromTexture(pos.AddX(41), "scenery/car/barrier") with {
                Origin = new(0.0f, 1.0f),
                Depth = 5,
                Color = Color.DarkGray
            };
        }
    }

    public static FieldList GetFields() => new(new {
        hasRoadAndBarriers = false
    });

    public static PlacementList GetPlacements() => new("intro_car");
}