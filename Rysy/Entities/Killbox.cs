using Rysy;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("killbox")]
public sealed class Killbox : Entity, IPlaceable {
    public override int Depth => 0;

    public override bool ResizableX => true;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.OutlinedRect(Pos, Width, 8, Color.Red * 0.4f, Color.Red * 0.8f);
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("killbox");
}