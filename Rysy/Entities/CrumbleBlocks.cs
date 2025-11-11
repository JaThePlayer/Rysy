using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("crumbleBlock")]
public class CrumbleBlocks : LoopingSpriteSliceEntity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;
    public override string TexturePath => $"objects/crumbleBlock/{Attr("texture", "default")}";
    public override int TileSize => 8;
    public override LoopingMode LoopMode => LoopingMode.PickRandom; // TODO: this is not how it works in vanilla, it just loops over the texture there

    public override bool ResizableX => true;

    public static FieldList GetFields() => new(new {
        texture = new PathField("default", Gfx.Atlas, @"objects/crumbleBlock/(.*)").AllowEdits(),  //Fields.Dropdown("default", () => KnownPaths.Value.Value, editable: true),
    });

    public static PlacementList GetPlacements() => new("default");
}
