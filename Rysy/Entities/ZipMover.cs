using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("zipMover")]
public class ZipMover : Entity, ISolid
{
    public override int Depth => Depths.Solids;

    public override IEnumerable<ISprite> GetSprites()
    {
        var w = Width;
        var h = Height;

        yield return ISprite.Rect(Pos, w, h, Color.Black);

        foreach (var item in ISprite.GetNineSliceSprites(ISprite.FromTexture(Pos, "objects/zipmover/block"), Pos, w / 8, h / 8, 8))
        {
            yield return item;
        }

        yield return ISprite.FromTexture(Pos + new Vector2(w / 2, 0), "objects/zipmover/light00") with
        {
            Origin = new(.5f, 0f)
        };
    }
}
