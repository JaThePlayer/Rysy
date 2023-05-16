using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("booster")]
public class Booster : SpriteEntity, IPlaceable {
    public override int Depth => -8500;
    public override string TexturePath => Bool("red", false) ? "objects/booster/boosterRed00" : "objects/booster/booster00";

    public static FieldList GetFields() => new(new {
        red = false,
        ch9_hub_booster = false
    });

    public static PlacementList GetPlacements() => new() {
        new("green"),
        new("red", new {
            red = true,
        }),
    };
}
