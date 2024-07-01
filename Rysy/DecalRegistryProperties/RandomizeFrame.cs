using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("randomizeFrame")]
public sealed class RandomizeFrameDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("default");
}