using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("coreSwap")]
public sealed class CoreSwapDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        coldPath = Fields.AtlasPath("", "(.*)"),
        hotPath = Fields.AtlasPath("", "(.*)"),
    });

    public static PlacementList GetPlacements() => new("default");
}