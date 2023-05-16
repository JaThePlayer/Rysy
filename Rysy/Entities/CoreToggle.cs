using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("coreModeToggle")]
internal class CoreToggle : SpriteEntity, IPlaceable {
    public override int Depth => 2000;

    public override string TexturePath => Bool("onlyFire", false) ? "objects/coreFlipSwitch/switch15"
                                        : Bool("onlyIce", false) ? "objects/coreFlipSwitch/switch13"
                                        : "objects/coreFlipSwitch/switch01";

    public static FieldList GetFields() => new(new {
        onlyIce = false,
        onlyFire = false,
        persistent = false
    });

    public static PlacementList GetPlacements() => new() {
        new("both"),
        new("fire", new {
            onlyFire = true,
        }),
        new("ice", new {
            onlyIce = true,
        }),
    };
}
