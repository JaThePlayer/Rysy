using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class SpriteEntity : Entity {
    /// <summary>
    /// The color to use to tint the sprite. Leave as <see cref="Color.White"/> to not apply any tinting.
    /// </summary>
    public virtual Color Color => Color.White;

    /// <summary>
    /// The color to use to render the outline. If the color is fully transparent (the default), the outline will not be drawn.
    /// </summary>
    public virtual Color OutlineColor => default;

    /// <summary>
    /// The path to the texture used to render this entity.
    /// </summary>
    public abstract string TexturePath { get; }

    /// <summary>
    /// The origin of the sprite. By default, this is (.5f, .5f), which centers the sprite.
    /// </summary>
    public virtual Vector2 Origin => new(.5f, .5f);

    /// <summary>
    /// The rotation of the sprite.
    /// </summary>
    public virtual float Rotation => 0f;

    /// <summary>
    /// The scale of the sprite.
    /// </summary>
    public virtual Vector2 Scale => Vector2.One;

    /// <summary>
    /// How many pixels to offset the sprite.
    /// </summary>
    public virtual Vector2 Offset => default;

    public override IEnumerable<ISprite> GetSprites() {
        yield return GetSprite();
    }

    /// <summary>
    /// Gets the <see cref="Sprite"/> used to render this entity. If <paramref name="texturePath"/> is provided, it'll be used instead of the <see cref="TexturePath"/> property.
    /// </summary>
    public Sprite GetSprite(string? texturePath = null) {
        texturePath ??= TexturePath;

        return ISprite.FromTexture(Pos + Offset, texturePath) with {
            Origin = Origin,
            OutlineColor = OutlineColor,
            Color = Color,
            Rotation = Rotation,
            Scale = Scale,
        };
    }
}
