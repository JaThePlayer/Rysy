using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("switchGate")]
public class SwitchGate : NineSliceEntity, ISolid {
    public override int Depth => Depths.Solids;
    public override int TileSize => 8;
    public override string TexturePath => $"objects/switchgate/{Attr("sprite", "block")}";

    public override string? CenterSpritePath => "objects/switchgate/icon00";
    public override Color CenterSpriteColor => "5fcde4".FromRGB();
    public override Color CenterSpriteOutlineColor => Color.Black;
}
