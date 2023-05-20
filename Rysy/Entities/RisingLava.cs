using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("risingLava")]
public sealed class RisingLava : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "@Internal@/rising_lava";

    public static FieldList GetFields() => new(new {
        intro = false
    });

    public static PlacementList GetPlacements() => new("rising_lava");
}