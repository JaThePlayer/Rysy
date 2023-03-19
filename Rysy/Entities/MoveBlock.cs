using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("moveBlock")]
public class MoveBlock : Entity {
    private static Color FillColor = "474070".FromRGB();

    public override int Depth => Depths.Solids;

    public override IEnumerable<ISprite> GetSprites() {
        var canSteer = Bool("canSteer", true);
        var direction = Enum("direction", Directions.Left);
        var w = Width;
        var h = Height;
        var center = Center;

        var (baseSpritePath, arrowPath) = direction switch {
            Directions.Right => ("objects/moveBlock/base_h", "objects/moveBlock/arrow00"),
            Directions.Left => ("objects/moveBlock/base_h", "objects/moveBlock/arrow04"),
            Directions.Up => ("objects/moveBlock/base_v", "objects/moveBlock/arrow02"),
            Directions.Down => ("objects/moveBlock/base_v", "objects/moveBlock/arrow06"),
            _ => throw new NotImplementedException(),
        };
        if (!canSteer) {
            baseSpritePath = "objects/moveBlock/base";
        }

        if (canSteer) {
            var baseSprite = ISprite.FromTexture("objects/moveBlock/button") with {
                Color = FillColor
            };
            switch (direction) {
                case Directions.Left or Directions.Right:
                    foreach (var item in ISprite.GetNineSliceSprites(baseSprite, Pos + new Vector2(5f, 1f), w / 8, 1, 8)) {
                        yield return item.Centered();
                    }
                    break;
                // TODO: sane helper functions for this that can handle picking tiles horizontally even when going vertically
                // TODO: figure out the weird offsets -- Rysy rendering is not equivalent to vanilla!!!!!!
                case Directions.Up or Directions.Down:
                    for (int y = 0; y < h / 8; y++) {
                        var subX = y == 0 ? 0 : y < (h / 8) - 1 ? 8 : 16;
                        var subSprite = baseSprite.CreateSubtexture(subX, 0, 8, 8).Centered();

                        // left sprite
                        yield return subSprite with {
                            Pos = Pos + new Vector2(1, y * 8 + 5f),
                            Rotation = MathHelper.PiOver2,
                            Scale = new Vector2(1f, -1f)
                        };

                        // right sprite
                        yield return subSprite with {
                            Pos = Pos + new Vector2(Width - 1f, y * 8 + 5f),
                            Rotation = MathHelper.PiOver2,
                            Scale = new Vector2(1f, 1f)
                        };
                    }
                    break;
            }
        }

        yield return ISprite.Rect(new(X + 3, Y + 3, w - 6, h - 6), FillColor);

        foreach (var item in ISprite.GetNineSliceSprites(ISprite.FromTexture(baseSpritePath), Pos, w / 8, h / 8, 8)) {
            yield return item;
        }

        yield return ISprite.Rect(new((int) center.X - 4, (int) center.Y - 4, 8, 8), FillColor);

        yield return ISprite.FromTexture(center, arrowPath).Centered();
    }

    public enum Directions {
        Left,
        Right,
        Up,
        Down
    }
}
