using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("depth")]
public sealed class DepthDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        value = Fields.Depth(0),
    });

    public static PlacementList GetPlacements() => new("default");
}