using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("touchSwitch")]
public class TouchSwitch : SpriteEntity {
    public override string TexturePath => "objects/touchswitch/icon00";

    public override int Depth => 2000;

    public override Color Color => "5fcde4".FromRGB();

    public override IEnumerable<ISprite> GetSprites() {
        var container = GetSprite("objects/touchswitch/container");
        yield return container with {
            Color = Color.Black,
            Pos = Pos.AddY(-1)
        };
        yield return container;
        yield return GetSprite("objects/touchswitch/icon00");
    }
}
