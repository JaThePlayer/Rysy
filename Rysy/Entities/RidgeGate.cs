using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("ridgeGate")]
public class RidgeGate : SpriteEntity, IPlaceable {
    public override string TexturePath => Attr("texture", null!) ?? (Bool("ridge", true) ? "objects/ridgeGate" : "objects/farewellGate");

    public override Vector2 Origin => new();

    public override Color Color => Color.White * 0.8f;

    public override int Depth => Depths.Below;

    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("objects/ridgeGate", "(.*)").AllowEdits(),
        strawberries = "",
        keys = ""
    });

    public static PlacementList GetPlacements() => new("ridge_gate");
}
