using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("depth")]
public sealed class DepthDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        value = Fields.Dropdown(0, Depths.AllDepths, editable: true),
    });

    public static PlacementList GetPlacements() => new("default");
}