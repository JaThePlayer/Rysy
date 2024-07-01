using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("mirror")]
public sealed class MirrorDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        keepOffsetsClose = false,
    });

    public static PlacementList GetPlacements() => new("default");
}