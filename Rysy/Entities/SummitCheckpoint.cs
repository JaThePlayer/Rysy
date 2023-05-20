using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("summitcheckpoint")]
public sealed class SummitCheckpoint : Entity, IPlaceable {
    public override int Depth => 8999;

    private string BackSprite(int digit) => $"scenery/summitcheckpoints/numberbg0{digit}";
    private string FrontSprite(int digit) => $"scenery/summitcheckpoints/number0{digit}";

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;
        var number = Int("number");

        var digit1 = number % 100 / 10;
        var digit2 = number % 10;

        yield return ISprite.FromTexture(pos, "scenery/summitcheckpoints/base02").Centered();
        yield return ISprite.FromTexture(pos.Add(-2, 4), BackSprite(digit1)).Centered();
        yield return ISprite.FromTexture(pos.Add(2, 4), BackSprite(digit2)).Centered();
        yield return ISprite.FromTexture(pos.Add(-2, 4), FrontSprite(digit1)).Centered();
        yield return ISprite.FromTexture(pos.Add(2, 4), FrontSprite(digit2)).Centered();
    }

    public static FieldList GetFields() => new(new {
        number = 0
    });

    public static PlacementList GetPlacements() => new("summit_checkpoint");
}