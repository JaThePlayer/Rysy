using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("theoCrystalPedestal")]
public sealed class TheoCrystalPedestal : SpriteEntity, IPlaceable {
    public override int Depth => 8998;

    public override string TexturePath => "characters/theoCrystal/pedestal";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("pedestal");
}