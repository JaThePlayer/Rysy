using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("flagSwap")]
public sealed class FlagSwapDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        flag = "",
        onPath = Fields.AtlasPath("", "(.*)"),
        offPath = Fields.AtlasPath("", "(.*)"),
    });

    public static PlacementList GetPlacements() => new("default");
}