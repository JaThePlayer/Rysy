using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("ridgeGate")]
public class RidgeGate : SpriteEntity {
    public override string TexturePath => Attr("texture", null!) ?? (Bool("ridge", true) ? "objects/ridgeGate" : "objects/farewellGate");

    public override Vector2 Origin => new();

    public override Color Color => Color.White * 0.8f;

    public override int Depth => Depths.Below;

    public override Range NodeLimits => 0..1;
}
