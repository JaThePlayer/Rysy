using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("plateau")]
public sealed class Plateau : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "scenery/fallplateau";

    public override Vector2 Origin => new(0f, 0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("plateau");
}