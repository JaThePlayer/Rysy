using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("checkpoint")]
internal class Checkpoint : Entity {
    public override int Depth => 9990;

#warning GetSprites
    public override IEnumerable<ISprite> GetSprites() {
        var bg = Attr("bg", "");

        string id2;
        if (string.IsNullOrWhiteSpace(bg)) {
            id2 = "objects/checkpoint/flash03";
        } else {
            id2 = $"objects/checkpoint/bg/{bg}";
        }

        yield return ISprite.FromTexture(Pos, id2) with {
            Origin = new(0.5f, 1f),
        };
    }
}
