using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("glider")]
public sealed class Glider : Entity, IPlaceable {
    public override int Depth => -5;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "objects/glider/idle0").Centered();

        if (Bool("bubble")) {
            var pos = Pos + new Vector2(-12, -5);
            for (int i = 0; i < 24; i++) {
                var color = Color.White * i switch {
                    > 2 and < 21 => 0.8f,
                    _ => 0.4f,
                };

                yield return ISprite.Point(pos + new Vector2(0, MathF.Sin(i * 0.2f) * 1.8f), color);

                pos.X++;
            }
        }
    }

    public static FieldList GetFields() => new(new {
        tutorial = false,
        bubble = false
    });

    public static PlacementList GetPlacements() => new() {
        new("normal"),
        new("floating", new {
            bubble = true
        }),
    };
}