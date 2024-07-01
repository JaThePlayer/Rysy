using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("overlay")]
public sealed class OverlayDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("default");
}