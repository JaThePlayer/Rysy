using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("sound")]
public sealed class SoundDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        @event = "",
    });

    public static PlacementList GetPlacements() => new("default");
}