using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("rotateSpinner")]
public sealed class RotateSpinner : Entity, IPlaceable {
    public override int Depth => -50;

    public override Range NodeLimits => 1..1;

    public override IEnumerable<ISprite> GetSprites()
        => MovingSpinnerHelper.GetSprites(this);

    public override IEnumerable<ISprite> GetNodePathSprites()
        => NodePathTypes.Circle(this, nodeIsCenter: true);

    public static FieldList GetFields() => new(new {
        clockwise = true,
        dust = false,
        star = false
    });

    public static PlacementList GetPlacements() => IterationHelper.EachPair(MovingSpinnerHelper.PlacementTemplates, IterationHelper.BoolValues)
        .SelectTuple((type, clockwise) => new Placement($"{type.Name}_{(clockwise ? "clockwise" : "counter_clockwise")}", new {
            clockwise = clockwise,
            dust = type.Dust,
            star = type.Star
        }))
        .ToPlacementList();
}