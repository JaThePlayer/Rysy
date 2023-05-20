using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("blackholeStrength")]
public sealed class BlackHoleStrength : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        strength = CelesteEnums.BlackHoleStrengths.Mild
    });

    public static PlacementList GetPlacements() => new("black_hole_strength");
}