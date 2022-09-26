using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("crumbleBlock")]
public class CrumbleBlocks : LoopingSpriteSliceEntity, ISolid
{
    public override int Depth => Depths.Solids;
    public override string TexturePath => $"objects/crumbleBlock/{Attr("texture", "default")}";
    public override int TileSize => 8;
    public override LoopingMode LoopMode => LoopingMode.PickRandom; // TODO: this is not how it works in vanilla, it just loops over the texture there
}
