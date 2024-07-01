using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("animation")]
public sealed class AnimationDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        frames = new ListField(new AnimationFrameField(), "0").WithSeparator(',')
    });

    public static PlacementList GetPlacements() => new("default");
}
