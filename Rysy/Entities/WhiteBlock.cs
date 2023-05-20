using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("whiteblock")]
public sealed class WhiteBlock : SpriteEntity, IPlaceable {
    public override int Depth => 8990;

    public override string TexturePath => "objects/whiteblock";

    public override Vector2 Origin => new(0f, 0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("white_block");
}