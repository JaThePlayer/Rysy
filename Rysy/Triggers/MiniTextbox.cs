using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("minitextboxTrigger")]
public sealed class MiniTextbox : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        dialog_id = "",
        mode = CelesteEnums.MiniTextboxModes.OnPlayerEnter,
        death_count = -1,
        only_once = true,
    });

    public static PlacementList GetPlacements() => new("mini_text_box");
}