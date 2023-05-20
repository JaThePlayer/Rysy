using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("swapBlock")]
public class SwapBlock : NineSliceEntity, ISolid, IPlaceable {
    public override int Depth => -9999;

    public override Range NodeLimits => 1..1;

    public Themes Theme => Enum("theme", Themes.Normal);

    public override string TexturePath => Theme switch {
        Themes.Moon => "objects/swapblock/moon/block",
        _ => "objects/swapblock/block",
    };

    public override string? CenterSpritePath => Theme switch {
        Themes.Moon => "objects/swapblock/moon/midBlock00",
        _ => "objects/swapblock/midBlock00",
    };

    public static FieldList GetFields() => new(new {
        theme = Themes.Normal
    });

    public static PlacementList GetPlacements() => IterationHelper.EachName<Themes>()
        .Select(t => new Placement(t.ToLowerInvariant(), new {
            theme = t,
        }))
        .ToPlacementList();

    public enum Themes {
        Normal,
        Moon
    }
}
