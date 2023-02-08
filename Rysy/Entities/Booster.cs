using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("booster")]
public class Booster : SpriteEntity, IPlaceable {
    public override int Depth => -8500;
    public override string TexturePath => Bool("red", false) ? "objects/booster/boosterRed00" : "objects/booster/booster00";

    public static FieldList GetFields() => new() { 
        ["red"] = Fields.Bool(false),
    };

    public static List<Placement>? GetPlacements() => new() {
        new("Booster (Green)"),
        new("Booster (Red)") {
            ["red"] = true,
        }
    };
}
