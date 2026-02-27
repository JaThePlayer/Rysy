using Rysy.Graphics;

namespace Rysy.Components;

public interface IRoomSpriteProvider {
    public IReadOnlyList<ISprite> GetSprites(Room room);
}