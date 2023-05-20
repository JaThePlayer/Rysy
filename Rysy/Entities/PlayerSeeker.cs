using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("playerSeeker")]
public sealed class PlayerSeeker : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "decals/5-temple/statue_e";

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("player_seeker");
}