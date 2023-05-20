using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("templeBigEyeball")]
public sealed class TempleBigEyeball : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "danger/templeeye/body00").Centered();
        yield return ISprite.FromTexture(Pos, "danger/templeeye/pupil").Centered();
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("temple_big_eyeball");
}