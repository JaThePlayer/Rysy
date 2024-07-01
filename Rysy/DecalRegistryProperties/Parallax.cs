using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("parallax")]
public sealed class ParallaxDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        amount = 0.1f,
    });

    public static PlacementList GetPlacements() => new("default");
}