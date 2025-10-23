using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("blackholeStrength")]
public sealed class BlackHoleStrength : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Visual;
    
    public static FieldList GetFields() => new(new {
        strength = CelesteEnums.BlackHoleStrengths.Mild
    });

    public static PlacementList GetPlacements() => new("black_hole_strength");
}