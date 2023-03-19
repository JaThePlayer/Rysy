using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;


public static class ClutterBlockHelper {
    public static IEnumerable<ISprite> GetSprites(Entity ent, string texturePath) {
        var pos = ent.Pos;
        var w = ent.Width / 8;
        var h = ent.Height / 8;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var newPos = pos + new Vector2(x * 8, y * 8);
                yield return ISprite.FromTexture(newPos, $"{texturePath}_0{newPos.SeededRandomInclusive(0, 5)}");
            }
        }
    }
}

[CustomEntity("yellowBlocks")]
public sealed class YellowBlocks : Entity, ISolid {
    public override int Depth => -9998;

    public override IEnumerable<ISprite> GetSprites() => ClutterBlockHelper.GetSprites(this, "objects/resortclutter/yellow");
}

[CustomEntity("greenBlocks")]
public sealed class GreenBlocks : Entity, ISolid {
    public override int Depth => -9998;

    public override IEnumerable<ISprite> GetSprites() => ClutterBlockHelper.GetSprites(this, "objects/resortclutter/green");
}

[CustomEntity("redBlocks")]
public sealed class RedBlocks : Entity, ISolid {
    public override int Depth => -9998;

    public override IEnumerable<ISprite> GetSprites() => ClutterBlockHelper.GetSprites(this, "objects/resortclutter/red");
}
