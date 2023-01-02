using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("templeGate")]
public class TempleGate : SpriteEntity
{
    public override string TexturePath => Attr("sprite", "default") switch
    {
        "mirror" => "objects/door/templeDoorB00",
        "theo" => "objects/door/templeDoorC00",
        _ => "objects/door/templeDoor00",
    };

    public override Vector2 Origin => new(0.5f, 0f);

    public override Vector2 Offset => new(4f, 0f);

    public override IEnumerable<ISprite> GetSprites()
    {
        yield return ISprite.Rect(Pos - new Vector2(2f, 8f), 14, 10, Color.Black);
        yield return GetSprite();
    }

    public override int Depth => -9000;
}
