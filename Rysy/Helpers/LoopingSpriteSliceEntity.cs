using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class LoopingSpriteSliceEntity : SpriteEntity {
    public abstract int TileSize { get; }

    public abstract LoopingMode LoopMode { get; }

    public override IEnumerable<ISprite> GetSprites() {
        var tileSize = TileSize;
        var path = TexturePath;
        var mode = LoopMode;

        var baseSprite = GetSprite(path);

        var w = Width;
        if (w > 0) {
            var count = w / tileSize;

            for (int i = 0; i < count; i++) {
                yield return baseSprite.CreateSubtexture(
                    GetSubtextStartPos(ref baseSprite, mode, i, tileSize, count, false),
                    0, tileSize, tileSize) with {
                    Pos = Pos.AddX(i * tileSize),
                    Origin = new(),
                };
            }
        }

        var h = Height;
        if (h > 0) {
            var count = h / tileSize;

            for (int i = 0; i < count; i++) {
                yield return baseSprite.CreateSubtexture(
                    0, GetSubtextStartPos(ref baseSprite, mode, i, tileSize, count, true),
                    tileSize, tileSize) with {
                    Pos = Pos.AddY(i * tileSize),
                    Origin = new(),
                };
            }
        }
    }

    private int GetSubtextStartPos(ref Sprite baseSprite, LoopingMode mode, int i, int tileSize, int count, bool vertical)
        => mode switch {
            LoopingMode.RepeatFirstTile => 0,
            LoopingMode.UseEdgeTiles_RepeatMiddle => i switch {
                0 => 0,
                _ when i == count - 1 => tileSize * 2,
                _ => tileSize
            },
            LoopingMode.PickRandom => Pos.SeededRandomInclusive(0, (vertical ? baseSprite.Texture.Height : baseSprite.Texture.Width) / tileSize) * tileSize,
            _ => throw new Exception($"Unknown LoopingMode {mode}")
        };

    public enum LoopingMode {
        RepeatFirstTile,
        UseEdgeTiles_RepeatMiddle,
        PickRandom,
    }
}
