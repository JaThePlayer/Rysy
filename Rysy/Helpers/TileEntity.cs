using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class TileEntity : Entity {
    public abstract char Tiletype { get; }
    public abstract TileLayer Layer { get; }

    public virtual Color Color => Color.White * .68f;

    public override IEnumerable<ISprite> GetSprites()
        => GetSprites(Pos, Layer, Tiletype);

    public IEnumerable<ISprite> GetSprites(Vector2 pos, TileLayer layer, char tiletype)
        => GetTilegrid(Room, layer).Autotiler!.GetFilledRectSprites(pos, tiletype, Width / 8, Height / 8, Color);

    public static Tilegrid GetTilegrid(Room room, TileLayer layer) {
        ArgumentNullException.ThrowIfNull(room);

        return layer switch {
            TileLayer.Bg => room.Bg,
            TileLayer.Fg => room.Fg,
            _ => throw new Exception($"Unknown TileLayer: {layer}")
        };
    }

    public static Autotiler GetAutotiler(Map map, TileLayer layer) {
        ArgumentNullException.ThrowIfNull(map);
        return layer switch {
            TileLayer.Bg => map.BgAutotiler,
            TileLayer.Fg => map.FgAutotiler,
            _ => throw new Exception($"Unknown TileLayer: {layer}")
        };
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public override Point MinimumSize => new(8, 8);

}
