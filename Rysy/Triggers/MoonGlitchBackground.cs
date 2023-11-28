using Rysy.Helpers;

namespace Rysy.Triggers; 

[CustomEntity("moonGlitchBackgroundTrigger")]
public sealed class MoonGlitchBackground : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        duration = CelesteEnums.MoonGlitchBackgroundDurations.Short,
        stay = false,
        glitch = true
    });

    public static PlacementList GetPlacements() => new("moon_glitch_background");
}