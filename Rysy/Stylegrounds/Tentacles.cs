using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Stylegrounds;

[CustomEntity("tentacles")]
public sealed class Tentacles : Style, IPlaceable {
    public static FieldList GetFields() => new(new {
        color = Fields.Rgb(null!).AllowNull(),
        side = TentacleEffectDirections.Right,
        offset = 0.0f
    });

    public static PlacementList GetPlacements() => new("default");
}