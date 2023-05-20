using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("SummitBackgroundManager")]
public sealed class SummitBackgroundManager : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "@Internal@/summit_background_manager";

    public static FieldList GetFields() => new(new {
        index = 0,
        cutscene = "",
        intro_launch = false,
        dark = false,
        ambience = ""
    });

    public static PlacementList GetPlacements() => new("manager");
}