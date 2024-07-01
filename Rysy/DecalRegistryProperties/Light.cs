using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("light")]
public sealed class LightDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public override bool AllowMultiple => true;

    public static FieldList GetFields() => new(new {
        offsetX = 0f,
        offsetY = 0f,
        color = Fields.RGB("ffffff").AllowNull(),
        alpha = Fields.Float(1f).WithMin(0f).WithMax(1f),
        startFade = Fields.Int(16).WithMin(0),
        endFade = Fields.Int(24).WithMin(0)
    });

    public static PlacementList GetPlacements() => new("default");
}