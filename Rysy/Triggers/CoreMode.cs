using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("everest/coreModeTrigger")]
public sealed class CoreMode : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        mode = CelesteEnums.CoreModes.None,
        playEffects = true
    });

    public static PlacementList GetPlacements() => new("core_mode");
}