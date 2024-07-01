using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("animationSpeed")]
public sealed class AnimationSpeedDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        value = 12,
    });

    public static PlacementList GetPlacements() => new("default");
}