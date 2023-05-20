using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("strawberry")]
public class Strawberry : SpriteEntity, IPlaceable {
    public override string TexturePath => Moon 
        ? "collectables/moonBerry/normal00"
        : $"collectables/strawberry/{(Winged ? "wings" : "normal")}00";

    public override int Depth => Depths.Top;

    public override Range NodeLimits => 0..;

    public bool Winged => Bool("winged");

    public bool Moon => Bool("moon");

    public override IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.Fan(this);

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        yield return GetSprite("collectables/strawberry/seed00") with {
            Pos = Nodes![nodeIndex]
        };
    }

    public static FieldList GetFields() => new(new {
        winged = false,
        moon = false,
        checkpointID = -1,
        order = -1
    });

    public static PlacementList GetPlacements() => new() {
        new("normal"),
        new("normal_winged", new {
            winged = true
        }),
        new("moon", new {
            moon = true,
        }),
    };
}
