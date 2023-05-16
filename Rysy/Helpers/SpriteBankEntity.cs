using Rysy.Graphics;

namespace Rysy.Helpers;

/// <summary>
/// Represents an entity which gets rendered using a SpriteBank entry
/// </summary>
public abstract class SpriteBankEntity : Entity {
    public abstract string SpriteBankEntry { get; }
    public abstract string Animation { get; }
    public virtual SpriteBank? SpriteBank => null;
    public virtual Vector2 Offset => default;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromSpriteBank(Pos + Offset, SpriteBankEntry, Animation, SpriteBank);
    }
}
