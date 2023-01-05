using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class LoopingSpriteEntity : SpriteEntity {
    /// <summary>
    /// Overrides the spacing between each instance of the sprite.
    /// If null (default), it'll be equal to the width or height of the texture
    /// </summary>
    public virtual int? SpriteSpacingOverride => null;

    public override IEnumerable<ISprite> GetSprites() {
        var baseSprite = GetSprite(TexturePath);

        var w = Width;
        if (w > 0) {
            var tileSize = SpriteSpacingOverride ?? baseSprite.Texture.Width;

            var count = w / tileSize;
            return Enumerable.Range(0, count).Select<int, ISprite>(i =>
            baseSprite with {
                Pos = baseSprite.Pos + new Vector2(i * tileSize, 0f),
            });
        }

        var h = Height;
        if (h > 0) {
            var tileSize = SpriteSpacingOverride ?? baseSprite.Texture.Width;
            var count = h / tileSize;
            return Enumerable.Range(0, count).Select<int, ISprite>(i =>
            baseSprite with {
                Pos = baseSprite.Pos + new Vector2(0f, i * tileSize),
            });
        }

        return new List<ISprite>();
    }
}
