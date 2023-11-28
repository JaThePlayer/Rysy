using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("everest/crystalShatterTrigger")]
public sealed class CrystalShatter : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        mode = CelesteEnums.CrystalShatterTriggerModes.All,
    });

    public static PlacementList GetPlacements() => new("crystal_shatter");
}