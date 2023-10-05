using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("eyebomb")]
public class Puffer : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "objects/puffer/idle00";

    public override Color OutlineColor => Color.Black;

    public virtual int MinIndicatorIndex => 0;
    public virtual int MaxIndicatorIndex => 28;

    public virtual Color IndicatorColor => Color.White * 0.75f;

    public static FieldList GetFields() => new(new {
        right = false
    });

    public static PlacementList GetPlacements() => new() {
        new("left"),
        new("right", new {
            right = true
        }),
    };

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl["right"] = !Bool("right"));

    public override IEnumerable<ISprite> GetSprites() {
        yield return GetSprite() with {
            Scale = new(Bool("right", false) ? 1 : -1, 1),
        };

        var pos = Pos;
        var minIndex = MinIndicatorIndex;
        var maxIndex = MaxIndicatorIndex;
        var color = IndicatorColor;

        for (float i = minIndex; i < maxIndex; i++) {
            var angle = MathHelper.Lerp(0f, 186f, i / maxIndex).ToRad().AngleToVector(1f);

            if (i == minIndex || i == maxIndex - 1) {
                yield return ISprite.LineFloored(pos + angle * 32f, pos + angle * 22f, color);
            } else {
                yield return ISprite.Rect(pos + angle * 32f, 1, 1, color);
            }
        }
    }
}
