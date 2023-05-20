using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("theoCrystal")]
public sealed class TheoCrystal : SpriteEntity, IPlaceable {
    public override int Depth => 100;

    public override string TexturePath => "characters/theoCrystal/idle00";

    public override Vector2 Offset => new(0, -10);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("theo_crystal");
}