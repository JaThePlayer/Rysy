using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("floatingDebris")]
public class FloatingDebris : SpriteEntity {
    public override int Depth => -5;
    public override string TexturePath => "scenery/debris";

    public override IEnumerable<ISprite> GetSprites() {
        var sprite = GetSprite();
        yield return sprite.CreateSubtexture(Pos.SeededRandomExclusive(sprite.Texture.Width / 8) * 8, 0, 8, 8);
    }
}
