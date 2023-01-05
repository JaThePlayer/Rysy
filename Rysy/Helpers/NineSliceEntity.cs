using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class NineSliceEntity : SpriteEntity {
    public virtual string? CenterSpritePath => null;
    public virtual Color CenterSpriteColor => Color.White;
    public virtual Color CenterSpriteOutlineColor => Color.Transparent;

    public abstract int TileSize { get; }

    public override IEnumerable<ISprite> GetSprites() {
        var baseSprite = GetSprite();

        var tileSize = TileSize;
        var w = Math.Max(Width, 8) / 8;
        var h = Math.Max(Height, 8) / 8;

        foreach (var item in ISprite.GetNineSliceSprites(baseSprite, Pos, w, h, tileSize))
            yield return item;

        if (CenterSpritePath is { } centerPath) {
            yield return GetSprite(centerPath) with {
                Pos = Pos + new Vector2(w * 8 / 2, h * 8 / 2),
                Color = CenterSpriteColor,
                OutlineColor = CenterSpriteOutlineColor,
            };
        }
    }
}
