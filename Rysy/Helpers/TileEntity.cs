using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class TileEntity : Entity {
    public abstract char Tiletype { get; }
    public abstract TileLayer Layer { get; }

    public virtual Color Color => Color.White * .68f;

    public override IEnumerable<ISprite> GetSprites()
        => GetSprites(Pos, Layer, Tiletype);

    public IEnumerable<ISprite> GetSprites(Vector2 pos, TileLayer layer, char tiletype)
        => GetTilegrid(layer).Autotiler!.GetSprites(pos, tiletype, Width / 8, Height / 8, Color);

    public Tilegrid GetTilegrid(TileLayer layer) {
        return layer switch {
            TileLayer.BG => Room.BG,
            TileLayer.FG => Room.FG,
            _ => throw new Exception($"Unknown TileLayer: {Layer}")
        };
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public override Point MinimumSize => new(8, 8);

    public enum TileLayer {
        BG,
        FG
    }
}
