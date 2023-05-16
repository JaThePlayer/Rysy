using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("payphone")]
public sealed class Payphone : SpriteEntity, IPlaceable {
    public override int Depth => 1;

    public override string TexturePath => "scenery/payphone";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("payphone");
}