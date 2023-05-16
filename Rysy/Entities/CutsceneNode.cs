using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cutsceneNode")]
internal class CutsceneNode : SpriteEntity, IPlaceable {
    public override string TexturePath => "Rysy:cutscene_node";

    public override int Depth => 0;

    public static FieldList GetFields() => new(new {
        nodeName = ""
    });

    public static PlacementList GetPlacements() => new("cutscene_node");
}
