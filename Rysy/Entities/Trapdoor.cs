using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("trapdoor")]
public sealed class Trapdoor : SpriteBankEntity, IPlaceable {
    public override int Depth => 8999;

    public override string SpriteBankEntry => "trapdoor";

    public override string Animation => "idle";

    public override Vector2 Offset => new(0f, 6f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("trapdoor");
}