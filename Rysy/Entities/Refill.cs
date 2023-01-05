using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("refill")]
public class Refill : SpriteEntity {
    public override int Depth => -100;

    public override Color OutlineColor => Color.Black;

    public override string TexturePath =>
        Bool("twoDash", false)
            ? "objects/refillTwo/idle00"
            : "objects/refill/idle00";
}
