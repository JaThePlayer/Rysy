using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class TilegridEntity : Entity {
    public virtual TileLayer Layer => TileLayer.FG;

    public virtual Color Color => Color.White;

    public override Point MinimumSize => new(8, 8);

    public override int Depth => Layer.Depth - 1;

    public virtual char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => Tilegrid.TileArrayFromString(widthTiles * 8, heightTiles * 8, gridString);

    public override IEnumerable<ISprite> GetSprites() {
        if (CachedSprites is { })
            return CachedSprites;

        var tileData = Attr("tileData", "");
        if (string.IsNullOrWhiteSpace(tileData)) {
            return ISprite.OutlinedRect(Rectangle, Color.OrangeRed * 0.3f, Color.OrangeRed);
        }

        var tiles = ParseTilegrid(tileData, Width / 8, Height / 8);

        return CachedSprites = TileEntity.GetTilegrid(Room, Layer).Autotiler!.GetSprites(Pos, tiles, Color, tilesOOB: false);
    }

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        CachedSprites = null;
    }

    public override Entity? TryFlipHorizontal()
        => Manip(tiles => tiles.CreateFlippedHorizontally());

    public override Entity? TryFlipVertical()
        => Manip(tiles => tiles.CreateFlippedVertically());

    public Entity? Manip(Func<char[,], char[,]> tileManipulator) {
        var tileData = Attr("tileData", "");
        if (string.IsNullOrWhiteSpace(tileData)) {
            return null;
        }

        var tiles = ParseTilegrid(tileData, Width / 8, Height / 8);
        var newTiles = tileManipulator(tiles);

        return CloneWith(pl => pl["tileData"] = Tilegrid.GetSaveString(newTiles));
    }

    public Entity ChangeTilesTo(char[,] newTiles) {
        int offX = 0, offY = 0;

        var cloned = CloneWith(pl => {
            var trimmed = newTiles.CreateTrimmed('0', out offX, out offY);

            pl["tileData"] = Tilegrid.GetSaveString(trimmed);
            pl["width"] = trimmed.GetLength(0) * 8;
            pl["height"] = trimmed.GetLength(1) * 8;
        });

        cloned.Pos += new Vector2(offX * 8, offY * 8);

        return cloned;
    }

    private AutotiledSpriteList? CachedSprites;
}
