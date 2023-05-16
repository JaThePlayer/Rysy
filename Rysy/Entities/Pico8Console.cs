using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("picoconsole")]
public sealed class Pico8Console : SpriteEntity, IPlaceable {
    public override int Depth => 1000;

    public override string TexturePath => "objects/pico8Console";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("pico_console");
}