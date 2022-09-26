using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("jumpThru")]
public class Jumpthru : Entity
{
    public override int Depth => -9000;

    public override IEnumerable<ISprite> GetSprites()
    {
        var type = Attr("texture") switch
        {
            "default" or "" => "wood",
            var other => other,
        };

        var baseSprite = ISprite.FromTexture(Pos, $"objects/jumpthru/{type}");

        var count = Width / 8;
        for (int i = 0; i < count; i++)
        {
            var p = Pos.AddX(8 * i);

            var (subX, subY) = i switch
            {
                // left
                0 => (0, Room.IsSolidAt(p.AddX(-8)) ? 0 : 8),
                // right
                _ when i == count - 1 => (16, Room.IsSolidAt(p.AddX(8)) ? 0 : 8),
                // middle
                _ => (8, 8),
            };

            yield return baseSprite.CreateSubtexture(subX, subY, 8, 8) with
            {
                Pos = p,
            };
        }
    }
}
