using Rysy;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("sandwichLava")]
public sealed class LavaSandwich : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "@Internal@/lava_sandwich";

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("lava_sandwich");
}