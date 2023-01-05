using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("darkChaser")]
public class BadelineChaser : SpriteEntity {
    public override string TexturePath => "characters/badeline/sleep00";

    public override int Depth => 0;

    public override Vector2 Origin => new(.5f, 1f);
}
