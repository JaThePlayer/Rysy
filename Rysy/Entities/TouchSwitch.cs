using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("touchSwitch")]
public class TouchSwitch : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/touchswitch/icon00";

    public override int Depth => 2000;

    public override Color Color => "5fcde4".FromRgb();

    public override IEnumerable<ISprite> GetSprites() {
        var container = GetSprite("objects/touchswitch/container");
        yield return container;
        yield return container with {
            Color = Color.Black,
            Pos = Pos.AddY(-1),
            Depth = Depth + 1,
        };
        yield return GetSprite("objects/touchswitch/icon00");
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("touch_switch");
}
