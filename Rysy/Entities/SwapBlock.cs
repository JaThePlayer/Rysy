using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("swapBlock")]
public class SwapBlock : NineSliceEntity
{
    public override int TileSize => 8;

    public Themes Theme => Enum("theme", Themes.Normal);

    public override string TexturePath => Theme switch
    {
        Themes.Moon => "objects/swapblock/moon/block",
        _ => "objects/swapblock/block",
    };

    public override int Depth => -9999;

    public override string? CenterSpritePath => Theme switch
    {
        Themes.Moon => "objects/swapblock/moon/midBlock00",
        _ => "objects/swapblock/midBlock00",
    };


    public enum Themes
    {
        Normal,
        Moon
    }
}
