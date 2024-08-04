using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("moveBlock")]
public class MoveBlock : Entity, IPlaceable {
    private static Color FillColor = "474070".FromRGB();

    public override int Depth => Depths.Solids;
    
    public override Point RecommendedMinimumSize => new(16, 16);

    public Directions Direction => Enum("direction", Directions.Left);

    public override IEnumerable<ISprite> GetSprites() {
        var canSteer = Bool("canSteer", true);
        var direction = Direction;
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
            switch (direction) {
                case Directions.Left or Directions.Right:
                    yield return ISprite.NineSliceFromTexture(Pos + new Vector2(0, -4f), w, 8, "objects/moveBlock/button") with {
                        Color = FillColor
                    };
                    break;
                case Directions.Up or Directions.Down:
                    var baseSprite = ISprite.FromTexture(Pos, "objects/moveBlock/button") with {
                        Color = FillColor
                    };

                    for (int y = 0; y < h / 8; y++) {
                        var subX = y == 0 ? 0 : y < (h / 8) - 1 ? 8 : 16;
                        var subSprite = baseSprite.CreateSubtexture(subX, 0, 8, 8);
                        subSprite.Origin = new();

                        // left sprite
                        yield return subSprite with {
                            Pos = subSprite.Pos + new Vector2(-1, y * 8),
                            Rotation = MathHelper.PiOver2,
                            Scale = new Vector2(1f, -1f)
                        };

                        // right sprite
                        yield return subSprite with {
                            Pos = subSprite.Pos + new Vector2(Width + 3, y * 8),
                            Rotation = MathHelper.PiOver2,
                            Scale = new Vector2(1f, 1f)
                        };
                    }
                    break;
            }
        }

        yield return ISprite.Rect(new(X + 3, Y + 3, w - 6, h - 6), FillColor);

        yield return ISprite.NineSliceFromTexture(Rectangle, baseSpritePath);

        yield return ISprite.Rect(new((int) center.X - 4, (int) center.Y - 4, 8, 8), FillColor);

        yield return ISprite.FromTexture(center, arrowPath).Centered();
    }

    public override Entity? TryFlipHorizontal() => Direction switch {
        Directions.Right => CloneWith(pl => pl["direction"] = Directions.Left),
        Directions.Left => CloneWith(pl => pl["direction"] = Directions.Right),
        _ => null,
    };

    public override Entity? TryFlipVertical() => Direction switch {
        Directions.Up => CloneWith(pl => pl["direction"] = Directions.Down),
        Directions.Down => CloneWith(pl => pl["direction"] = Directions.Up),
        _ => null,
    };

    public override Entity? TryRotate(RotationDirection dir) => CloneWith(pl => pl["direction"] = dir.AddRotationTo(Direction));

    public static FieldList GetFields() => new() {
        ["direction"] = Fields.EnumNamesDropdown(Directions.Left),
        ["canSteer"] = Fields.Bool(true),
        ["fast"] = Fields.Bool(false),
    };

    public static PlacementList GetPlacements() => 
        IterationHelper.EachPair(System.Enum.GetNames<Directions>(), IterationHelper.BoolValues, IterationHelper.BoolValues)
        .SelectTuple((dir, steerable, fast) => new Placement($"{dir.ToLowerInvariant()}_{(steerable ? "steer" : "nosteer")}_{(fast ? "fast" : "slow")}", new {
            direction = dir,
            canSteer = steerable,
            fast = fast
        }))
        .ToPlacementList();

    public enum Directions {
        Up,
        Right,
        Down,
        Left,
    }
}
