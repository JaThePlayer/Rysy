using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class SpriteEntity : Entity
{
    public virtual Color Color => Color.White;
    public virtual Color OutlineColor => default;

    public abstract string TexturePath { get; }

    public virtual Vector2 Origin => new(.5f, .5f);

    public virtual float Rotation => 0f;

    public virtual Vector2 Offset => default;

    public override IEnumerable<ISprite> GetSprites()
    {
        yield return GetSprite();
    }

    public Sprite GetSprite(string? texturePath = null)
    {
        texturePath ??= TexturePath;

        return ISprite.FromTexture(Pos + Offset, texturePath) with
        {
            Origin = Origin,
            OutlineColor = OutlineColor,
            Color = Color,
            Rotation = Rotation,
        };
    }
}
