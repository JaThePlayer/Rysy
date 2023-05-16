using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("memorialTextController")]
public sealed class MemorialTextController : SpriteEntity, IPlaceable {
    public override int Depth => -100;

    public override string TexturePath => "collectables/goldberry/wings01";

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("golden_no_dash");
}