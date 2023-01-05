using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("templeEye")]
public class TempleEye : Entity {
    public bool IsBg => !Room.IsSolidAt(Pos);

    public override int Depth => IsBg ? 8990 : -10001;

    public override IEnumerable<ISprite> GetSprites() {
        if (IsBg) {
            yield return ISprite.FromTexture(Pos, "scenery/temple/eye/bg_eye").Centered();
            yield return ISprite.FromTexture(Pos, "scenery/temple/eye/bg_pupil").Centered();
        } else {
            yield return ISprite.FromTexture(Pos, "scenery/temple/eye/fg_eye").Centered();
            yield return ISprite.FromTexture(Pos, "scenery/temple/eye/fg_pupil").Centered();
        }
    }
}
