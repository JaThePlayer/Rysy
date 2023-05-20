using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("torch")]
public class Torch : SpriteEntity, IPlaceable {
    public override string TexturePath => Bool("startLit", false) ? "objects/temple/litTorch03" : "objects/temple/torch00";

    public override int Depth => 2000;

    public static FieldList GetFields() => new(new {
        startLit = false
    });

    public static PlacementList GetPlacements() => new("torch");
}
