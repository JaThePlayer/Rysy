using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("scared")]
public sealed class ScaredDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        hideRange = 32,
        showRange = 48,
        idleFrames = new ListField(new AnimationFrameField(), "0").WithSeparator(','),
        hideFrames = new ListField(new AnimationFrameField(), "0").WithSeparator(','),
        hiddenFrames = new ListField(new AnimationFrameField(), "0").WithSeparator(','),
        showFrames = new ListField(new AnimationFrameField(), "0").WithSeparator(','),
    });

    public static PlacementList GetPlacements() => new("default");
}