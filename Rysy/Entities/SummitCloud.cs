using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("summitcloud")]
public sealed class SummitCloud : Entity, IPlaceable {
    private static readonly string[] Textures = new[] {
        "scenery/summitclouds/cloud00",
        "scenery/summitclouds/cloud01",
        "scenery/summitclouds/cloud03"
    };

    public override int Depth => -10550;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos, pos.SeededRandomFrom(Textures)).Centered() with {
            Scale = new(pos.SeededRandomFrom(-1, 1), 1),
        };
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("summit_cloud");
}