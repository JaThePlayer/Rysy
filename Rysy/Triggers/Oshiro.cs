namespace Rysy.Triggers; 

[CustomEntity("oshiroTrigger")]
public sealed class Oshiro : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        state = true
    });

    public static PlacementList GetPlacements() => new() {
        new("oshiro_spawn", new {
            state = true
        }),
        new("oshiro_leave", new {
            state = false
        })
    };
}