using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("everest/starClimbGraphicsController")]
public sealed class StarClimbController : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "@Internal@/northern_lights";

    public static FieldList GetFields() => new(new {
        fgColor = Fields.Rgb("a3ffff"),
        bgColor = Fields.Rgb("293e4b")
    });

    public static PlacementList GetPlacements() => new("controller");
}