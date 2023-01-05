using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("blockField")]
public sealed class StrawberryBlockfield : Entity {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.OutlinedRect(Rectangle, Color.LightSkyBlue * 0.45f, Color.LightSkyBlue * 0.75f);

        var center = Center;
        yield return ISprite.FromTexture(center, "collectables/ghostberry/idle00").Centered() with {
            Color = Color.White * 0.5f
        };

        center.Y += 1;
        yield return ISprite.Line(center + new Vector2(4f), center + new Vector2(-4f), Color.Red * .7f);
        yield return ISprite.Line(center + new Vector2(4f, -4f), center + new Vector2(-4f, 4f), Color.Red * .7f);
    }
}
