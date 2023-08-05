namespace Rysy.Triggers;

[CustomEntity("musicFadeTrigger")]
public sealed class MusicFade : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        direction = Fields.Dropdown("leftToRight", new List<string>() { "leftToRight", "topToBottom" }),
        fadeA = 0.0,
        fadeB = 1.0,
        parameter = ""
    });

    public static PlacementList GetPlacements() => new("music_fade");
}