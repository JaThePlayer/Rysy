using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("friendlyGhost")]
public sealed class OshiroBoss : SpriteEntity, IPlaceable {
    public override int Depth => -12500;

    public override string TexturePath => "characters/oshiro/boss13";

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("oshiro_boss");
}