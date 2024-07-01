using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("smoke")]
public class SmokeDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public override bool AllowMultiple => true;

    public static FieldList GetFields() => new(new {
        offsetX = 1f,
        offsetY = 1f,
        inbg = false,
    });

    public static PlacementList GetPlacements() => new("default");
}