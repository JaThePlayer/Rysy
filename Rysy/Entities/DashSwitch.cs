using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("dashSwitchV")]
[CustomEntity("dashSwitchH")]
public class DashSwitch : Entity, IMultiSidPlaceable {
    public override int Depth => 0;

    Directions Direction => Name switch {
        "dashSwitchV" => Bool("ceiling") ? Directions.Ceiling : Directions.Floor,
        "dashSwitchH" => Bool("leftSide") ? Directions.Left : Directions.Right,
        _ => throw new NotImplementedException(Name)
    };

    public override Entity? TryFlipHorizontal() => Direction switch {
        Directions.Left => CloneWith(pl => pl["leftSide"] = false),
        Directions.Right => CloneWith(pl => pl["leftSide"] = true),
        _ => null,
    };

    public override Entity? TryFlipVertical() => Direction switch {
        Directions.Ceiling => CloneWith(pl => pl["ceiling"] = false),
        Directions.Floor => CloneWith(pl => pl["ceiling"] = true),
        _ => null,
    };

    public override Entity? TryRotate(RotationDirection dir) {
        var newDir = dir.AddRotationTo(Direction);

        var (sid, field, val) = newDir switch {
            Directions.Floor => ("dashSwitchV", "ceiling", false),
            Directions.Ceiling => ("dashSwitchV", "ceiling", true),
            Directions.Right => ("dashSwitchH", "leftSide", false),
            _ => ("dashSwitchH", "leftSide", true),
        };

        return CloneWith(pl => {
            pl.Sid = sid;
            pl["ceiling"] = null;
            pl["leftSide"] = null;
            pl[field] = val;
        });
    }

    public override IEnumerable<ISprite> GetSprites() {
        var sprite = ISprite.FromSpriteBank(Pos, $"dashSwitch_{Attr("sprite", "default")}", "idle");

        switch (Direction) {
            case Directions.Ceiling:
                sprite = sprite with {
                    Pos = sprite.Pos + new Vector2(8f, 0f),
                    Rotation = -MathHelper.Pi / 2,
                };
                break;
            case Directions.Floor:
                sprite = sprite with {
                    Pos = sprite.Pos + new Vector2(8f, 8f),
                    Rotation = MathHelper.Pi / 2,
                };
                break;
            case Directions.Left:
                sprite = sprite with {
                    Pos = sprite.Pos + new Vector2(0f, 8f),
                    Rotation = MathHelper.Pi,
                };
                break;
            case Directions.Right:
                sprite = sprite with {
                    Pos = sprite.Pos + new Vector2(8f, 8f),
                };
                break;
            default:
                break;
        }

        yield return sprite;
    }

    public static FieldList GetFields(string sid) => new(sid switch {
        "dashSwitchV" => new {
            sprite = Fields.SpriteBankPath("default", "^dashSwitch_(.*)", previewAnimation: "idle"),
            persistent = false,
            allGates = false,
            ceiling = false,
        },
        _ => new {
            sprite = Fields.SpriteBankPath("default", "^dashSwitch_(.*)", previewAnimation: "idle"),
            persistent = false,
            allGates = false,
            leftSide = false,
        }
    });

    public static PlacementList GetPlacements(string sid) => sid switch {
        "dashSwitchV" => new() {
            new("up_default", new {
                ceiling = false,
            }),
            new("down_default", new {
                ceiling = true,
            })
        },
        _ => new() {
            new("right_default", new {
                leftSide = false,
            }),
            new("left_default", new {
                leftSide = true,
            })
        }
    };

    private enum Directions {
        Floor = 0,
        Left = 1,
        Ceiling = 2,
        Right = 3,
    }
}
