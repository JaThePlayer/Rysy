using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("wavedashmachine")]
public sealed class InternetCafe : Entity, IPlaceable {
    public override int Depth => 1000;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos, "objects/wavedashtutorial/building_back") with {
            Origin = new(0.5f, 1f),
        };
        yield return ISprite.FromTexture(pos, "objects/wavedashtutorial/building_front_left") with {
            Origin = new(0.5f, 1f),
        };
        yield return ISprite.FromTexture(pos, "objects/wavedashtutorial/building_front_right") with {
            Origin = new(0.5f, 1f),
        };
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("cafe");
}