using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("eventTrigger")]
public sealed class EventTrigger : Trigger, IPlaceable {
    public static FieldList GetFields() => new() {
        ["event"] = Fields.Dropdown("", CelesteEnums.EventTriggerEvents.AsReadOnly(), editable: true),
    };

    public static PlacementList GetPlacements() => new("event");
}