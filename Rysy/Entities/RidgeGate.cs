using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("ridgeGate")]
public class RidgeGate : SpriteEntity, IPlaceable {
    public override string TexturePath => Attr("texture", Bool("ridge", true) ? "objects/ridgeGate" : "objects/farewellGate");

    public override Vector2 Origin => new();

    public override Color Color => Color.White * 0.8f;

    public override int Depth => Depths.Below;

    public override Range NodeLimits => 0..1;

    public override bool ResizableX => true;

    public override bool ResizableY => true;

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("objects/ridgeGate", "(.*)").AllowEdits(),
        strawberries = Fields.List("", Fields.EntityIdWithRoom()) with {
            MinElements = 0,
        },
        keys = Fields.List("", Fields.EntityIdWithRoom()) with {
            MinElements = 0,
        },
    });

    public static PlacementList GetPlacements() => [
        new Placement("ridge_gate").SetSize(32, 32)
    ];
}
