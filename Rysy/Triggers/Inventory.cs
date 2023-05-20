using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("everest/changeInventoryTrigger")]
public sealed class Inventory : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        inventory = CelesteEnums.Inventories.Default
    });

    public static PlacementList GetPlacements() => new("change_inventory");
}