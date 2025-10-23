using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("musicTrigger")]
public sealed class Music : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Audio;

    public static FieldList GetFields() => new(new {
        track = Fields.Dropdown("", CelesteEnums.Music, editable: true),
        resetOnLeave = true,
        progress = 0
    });

    public static PlacementList GetPlacements() => new("music");
}