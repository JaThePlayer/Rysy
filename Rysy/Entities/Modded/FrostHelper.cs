using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;

namespace Rysy.Entities.Modded;

[CustomEntity("FrostHelper/IceSpinner", associatedMods: [ "FrostHelper" ])]
internal sealed class CustomSpinner : LonnEntity {
    [Bind("attachToSolid")]
    public bool AttachToSolid;

    [Bind("attachGroup")]
    public int AttachGroup;

    [Bind("rainbow")]
    public bool Rainbow;

    [Bind("drawOutline")]
    public bool DrawOutline;

    [Bind("tint")]
    public Color Color;

    public Color BorderColor => RGBA("borderColor", Color.Black);

    private Sprite _fg, _bg;
    
    public override string? Documentation => "https://github.com/JaThePlayer/FrostHelper/wiki/Custom-Spinners";

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        if (!changed.OnlyPositionChanged)
            (_fg, _bg) = GetBaseSprites(Depth);
    }

    private static Dictionary<(string directory, string spritePathSuffix), (Sprite FG, Sprite BG)> SpriteCache = new();

    private (Sprite FG, Sprite BG) GetBaseSprites(int depth) {
        var directory = Attr("directory", "danger/FrostHelper/icecrystal");
        var suffix = Attr("spritePathSuffix", "");

        if (SpriteCache.TryGetValue((directory, suffix), out var cached)) {
            return cached;
        }

        var sprites = (
            ISprite.FromTexture(Pos, $"{directory}/fg{suffix}").Centered(),
            ISprite.FromTexture(Pos, $"{directory}/bg{suffix}").Centered() with {
                Depth = depth + 1,
            }
        );

        SpriteCache[(directory, suffix)] = sprites;

        return sprites;
    }

    public override IEnumerable<ISprite> GetSprites() {
        var depth = Depth;
        var color = Color;
        var attachToSolid = AttachToSolid;
        var attachGroup = AttachGroup;
        var pos = Pos;
        var rainbow = Rainbow;
        var drawOutline = DrawOutline;
        var borderColor = drawOutline ? BorderColor : default;
        var useOutlineTexture = drawOutline && borderColor == Color.Black;

        var (fgSprite, bgSprite) = (_fg, _bg);

        yield return fgSprite with {
            Color = rainbow ? ColorHelper.GetRainbowColor(Room, pos) : color,
            Pos = pos,
        };

        // the border has to be a separate sprite to render it at a different depth
        if (useOutlineTexture) {
            // use an outline texture to optimise rendering
            yield return fgSprite.WithOutlineTexture() with {
                Color = borderColor,
                Depth = depth + 2,
                Pos = pos,
            };
        }
        else if (drawOutline) {
            yield return fgSprite with {
                Color = Color.Transparent,
                OutlineColor = borderColor,
                Depth = depth + 2,
                Pos = pos,
            };
        }

        foreach (CustomSpinner spinner in Room.Entities[typeof(CustomSpinner)]) {
            if (spinner == this)
                break;

            var otherPos = spinner.Pos;

            if (Vector2.DistanceSquared(pos, otherPos) < 24f * 24f && spinner.AttachToSolid == attachToSolid && spinner.AttachGroup == attachGroup) {
                var connectorPos = (pos + otherPos) / 2f;

                yield return bgSprite with {
                    Pos = connectorPos,
                    Color = rainbow ? ColorHelper.GetRainbowColor(Room, connectorPos) : Color.Lerp(spinner.Color, color, 0.5f)
                };
                
                if (useOutlineTexture) {
                    // use an outline texture to optimise rendering
                    yield return bgSprite.WithOutlineTexture() with {
                        Pos = connectorPos,
                        Color = borderColor,
                        Depth = depth + 2,
                    };
                }
                else if (drawOutline) {
                    yield return bgSprite with {
                        Pos = connectorPos,
                        OutlineColor = borderColor,
                        Color = Color.Transparent,
                        Depth = depth + 2,
                    };
                }
            }
        }
    }
}

[CustomEntity("FrostHelper/RainbowTilesetController", associatedMods: [ "FrostHelper" ])]
internal sealed class RainbowTilesetController : LonnEntity, IPlaceable {
    [Bind("tilesets")]
    public IReadOnlyList<char> Tilesets;

    [Bind("bg")]
    public bool Bg;

    public TileLayer TileLayer => Bg ? TileLayer.BG : TileLayer.FG;
    
    public static FieldList GetFields() => new(new {
        tilesets = Fields.List("3", Fields.TileDropdown('3', ctx => ctx.GetValue("bg") is true)),
        bg = false,
    });

    public static PlacementList GetPlacements() => [];
}
