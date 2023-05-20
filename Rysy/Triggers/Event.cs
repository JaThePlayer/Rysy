using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("eventTrigger")]
public sealed class Event : Trigger, IPlaceable {
    public static FieldList GetFields() => new() {
        ["event"] = Fields.Dropdown("", CelesteEnums.EventTriggerEvents, editable: true),
    };

    public static PlacementList GetPlacements() => new("event");
}