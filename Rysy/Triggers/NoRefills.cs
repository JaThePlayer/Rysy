namespace Rysy.Triggers; 

[CustomEntity("noRefillTrigger")]
public sealed class NoRefills : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        state = true
    });

    public static PlacementList GetPlacements() => new() {
        new("disable_refills", new {
            state = true
        }),
        new("enable_refills", new {
            state = false
        })
    };
}