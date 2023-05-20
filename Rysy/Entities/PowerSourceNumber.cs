using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("powerSourceNumber")]
public sealed class PowerSourceNumber : Entity, IPlaceable {
    public override int Depth => -10010;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "scenery/powersource_numbers/1");
        yield return ISprite.FromTexture(Pos, "scenery/powersource_numbers/1_glow");
    }

    public static FieldList GetFields() => new(new {
        number = 1,
        strawberries = "",
        keys = ""
    });

    public static PlacementList GetPlacements() => new("power_source_number");
}