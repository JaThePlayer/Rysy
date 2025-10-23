using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("altMusicTrigger")]
public sealed class AltMusic : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Audio;

    public static FieldList GetFields() => new(new {
        track = Fields.Dropdown("", CelesteEnums.Music, editable: true),
        resetOnLeave = true
    });

    public static PlacementList GetPlacements() => new("alt_music");
}