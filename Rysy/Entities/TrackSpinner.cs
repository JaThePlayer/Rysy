using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("trackSpinner")]
public sealed class TrackSpinner : Entity, IPlaceable {
    public override int Depth => -50;

    public override Range NodeLimits => 1..1;

    public override IEnumerable<ISprite> GetSprites()
        => MovingSpinnerHelper.GetSprites(this);

    public static FieldList GetFields() => new(new {
        speed = Speeds.Normal,
        dust = false,
        star = false,
        startCenter = false
    });

    public static PlacementList GetPlacements() => IterationHelper.EachPair(MovingSpinnerHelper.PlacementTemplates, IterationHelper.EachName<Speeds>())
        .SelectTuple((template, speed) => new Placement($"{template.Name}_{speed.ToLowerInvariant()}", new {
            speed = speed,
            dust = template.Dust,
            star = template.Star,
        }))
        .ToPlacementList();

    public enum Speeds {
        Slow, Normal, Fast
    }
}