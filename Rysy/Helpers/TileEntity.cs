using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class TileEntity : Entity {
    public abstract char Tiletype { get; }
    public abstract TileLayer Layer { get; }

    public virtual Color Color => Color.White * .68f;

    public override IEnumerable<ISprite> GetSprites()
        => GetSprites(Pos, Layer, Tiletype);

    public IEnumerable<ISprite> GetSprites(Vector2 pos, TileLayer layer, char tiletype)
        => GetTilegrid(Room, layer).Autotiler!.GetSprites(pos, tiletype, Width / 8, Height / 8, Color);

    public static Tilegrid GetTilegrid(Room room, TileLayer layer) {
        ArgumentNullException.ThrowIfNull(room);

        return layer switch {
            TileLayer.BG => room.BG,
            TileLayer.FG => room.FG,
            _ => throw new Exception($"Unknown TileLayer: {layer}")
        };
    }

    public static Autotiler GetAutotiler(Map map, TileLayer layer) {
        ArgumentNullException.ThrowIfNull(map);
        return layer switch {
            TileLayer.BG => map.BGAutotiler,
            TileLayer.FG => map.FGAutotiler,
            _ => throw new Exception($"Unknown TileLayer: {layer}")
        };
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public override Point MinimumSize => new(8, 8);

}
