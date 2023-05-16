using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("key")]
public sealed class Key : SpriteEntity, IPlaceable {
    public override string TexturePath => "collectables/key/idle00";

    public override int Depth => -1000000;

    public override Range NodeLimits => Nodes is { Count: > 0 } ? 2..2 : 0..0;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new() { 
        new("normal"),
        new("with_return") {
            Nodes = new Vector2[2]
        },
    };
}
