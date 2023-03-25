using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class NineSliceEntity : Entity {
    /// <summary>
    /// The color to use to tint the nine slice. Leave as <see cref="Color.White"/> to not apply any tinting.
    /// </summary>
    public virtual Color Color => Color.White;

    /// <summary>
    /// The path to the texture used to render the nine slice.
    /// </summary>
    public abstract string TexturePath { get; }

    public virtual string? CenterSpritePath => null;
    public virtual Color CenterSpriteColor => Color.White;
    public virtual Color CenterSpriteOutlineColor => Color.Transparent;

    public override IEnumerable<ISprite> GetSprites() {
        var w = Math.Max(Width, 8);
        var h = Math.Max(Height, 8);

        yield return ISprite.NineSliceFromTexture(Pos, w, h, TexturePath) with {
            Color = Color,
        };

        if (CenterSpritePath is { } centerPath) {
            yield return ISprite.FromTexture(Pos + new Vector2(w / 2, h / 2), centerPath).Centered() with {
                Color = CenterSpriteColor,
                OutlineColor = CenterSpriteOutlineColor,
            };
        }
    }
}
