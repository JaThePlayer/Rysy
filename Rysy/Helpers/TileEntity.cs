using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class TileEntity : Entity {
    public abstract char Tiletype { get; }
    public abstract TileLayer Layer { get; }

    public virtual Color Color => Color.White * .68f;

    public override IEnumerable<ISprite> GetSprites() {
        var color = Color;
        var tileGrid = Layer switch {
            TileLayer.BG => Room.BG,
            TileLayer.FG => Room.FG,
            _ => throw new Exception($"Unknown TileLayer: {Layer}")
        };

        return tileGrid.Autotiler.GetSprites(Pos, Tiletype, Width / 8, Height / 8, Room.Random).Select(s => {
            if (s.Color == Color.White)
                s.Color = color;
            return s;
        });
    }

    public enum TileLayer {
        BG,
        FG
    }
}
