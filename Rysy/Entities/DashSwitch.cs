using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using System;

namespace Rysy.Entities;

[CustomEntity("dashSwitchV")]
[CustomEntity("dashSwitchH")]
public class DashSwitchV : Entity, IMultiSIDPlaceable {
    public override int Depth => 0;

    Directions Direction => Name switch {
        "dashSwitchV" => Bool("ceiling") ? Directions.Ceiling : Directions.Floor,
        "dashSwitchH" => Bool("leftSide") ? Directions.Left : Directions.Right,
        _ => throw new NotImplementedException(Name)
    };

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
            sprite = Fields.SpriteBankPath("default", "^dashSwitch_(.*)"),
            persistent = false,
            allGates = false,
            ceiling = false,
        },
        _ => new {
            sprite = Fields.SpriteBankPath("default", "^dashSwitch_(.*)"),
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
        Ceiling, Floor, Left, Right
    }
}
