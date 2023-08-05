using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Triggers;

[CustomEntity("everest/flagTrigger")]
public sealed class Flag : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        flag = "",
        state = true,
        mode = FlagTriggerModes.OnPlayerEnter,
        only_once = false,
        death_count = -1
    });

    public static PlacementList GetPlacements() => new("flag");
}